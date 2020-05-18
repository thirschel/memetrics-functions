using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1.Data;
using MeMetrics.Updater.Application.Interfaces;
using MeMetrics.Updater.Application.Objects;
using MeMetrics.Updater.Application.Objects.MeMetrics;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Serilog;
using Xunit;
using Message = Google.Apis.Gmail.v1.Data.Message;

namespace MeMetrics.Updater.Application.Tests
{
    public class MessageUpdaterTests
    {
        [Fact]
        public async Task GetAndSaveMessages_ShouldSaveMessagesSuccessfully()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Sms_Label = "SmsLabel",
                Gmail_Sms_Email_Address = "myEmail@address.com"
            });

            var messageId = "1";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Sms_Label, Id = config.Value.Gmail_Sms_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Sms_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                },
            });

            var occurredDate = "2020-1-1";

            gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Payload = new MessagePart() { 
                    Headers = new List<MessagePartHeader>()
                    {
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Date, Value = "Sat, 04 Apr 2020 11:47:41 -0500" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.From, Value = "Tess Ting <3128675309@unknown.email>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.To, Value = config.Value.Gmail_Sms_Email_Address },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.ThreadId, Value = "1" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.PhoneNumber, Value = "3128675309" },
                    },
                    Parts = new List<MessagePart>()
                    {
                        new MessagePart()
                        {
                            MimeType = Constants.EmailHeader.MimeType_Text,
                            Body = new MessagePartBody()
                            {
                                Data = "VGVzdA=="
                            }
                        }
                    }
                }
            });

            var expectedMessage = new Objects.MeMetrics.Message()
            {
                MessageId = messageId,
                OccurredDate = DateTimeOffset.Parse("Sat, 04 Apr 2020 11:47:41 -0500"),
                PhoneNumber = "13128675309",
                Name = "Tess Ting",
                IsIncoming = true,
                Text = "Test",
                IsMedia = false,
                TextLength = 4,
                ThreadId = 1,
                Attachments = new List<Attachment>()
            };

            Func<Objects.MeMetrics.Message, bool> validate = call => {
                Assert.Equal(JsonConvert.SerializeObject(expectedMessage), JsonConvert.SerializeObject(call));
                return true;
            };

            var updater = new MessageUpdater(loggerMock.Object, config, gmailApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveMessages();

            memetricsApiMock.Verify(x => x.SaveMessage(It.Is<Objects.MeMetrics.Message>(z => validate(z))), Times.Once);
        }

        [Fact]
        public async Task GetAndSaveMessages_ShouldOnlySaveMessage_IfMessageIsNotOlderThanTwoDays()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Sms_Label = "SmsLabel",
                Gmail_Sms_Email_Address = "myEmail@address.com"
            });

            var messageId = "1";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Sms_Label, Id = config.Value.Gmail_Sms_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Sms_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                },
            });


            gmailApiMock.Setup(x => x.GetEmail(messageId)).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.AddDays(-3).ToUnixTimeMilliseconds(),
            });


            var updater = new MessageUpdater(loggerMock.Object, config, gmailApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveMessages();

            memetricsApiMock.Verify(x => x.SaveMessage(It.IsAny<Objects.MeMetrics.Message>()), Times.Never);
        }

        [Fact]
        public async Task GetAndSaveMessages_ShouldUseNextPageToken_IfOutOfMessages()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Sms_Label = "SmsLabel",
                Gmail_Sms_Email_Address = "myEmail@address.com"
            });

            var messageId = "1";
            var secondMessageId = "2";
            var nextPageToken = "token";

            gmailApiMock.Setup(x => x.GetLabels()).ReturnsAsync(new ListLabelsResponse()
            {
                Labels = new List<Label>()
                {
                    new Label(){ Name = config.Value.Gmail_Sms_Label, Id = config.Value.Gmail_Sms_Label}
                }
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Sms_Label, null)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = messageId }
                },
                NextPageToken = nextPageToken
            });

            gmailApiMock.Setup(x => x.GetEmails(config.Value.Gmail_Sms_Label, nextPageToken)).ReturnsAsync(new ListMessagesResponse()
            {
                Messages = new List<Message>()
                {
                    new Message() { Id = secondMessageId }
                },
            });

            gmailApiMock.Setup(x => x.GetEmail(It.IsAny<string>())).ReturnsAsync(new Message()
            {
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Payload = new MessagePart()
                {
                    Headers = new List<MessagePartHeader>()
                    {
                        new MessagePartHeader(){ Name = Constants.EmailHeader.Date, Value = "Sat, 04 Apr 2020 11:47:41 -0500" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.From, Value = "Tess Ting <3128675309@unknown.email>" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.To, Value = config.Value.Gmail_Sms_Email_Address },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.ThreadId, Value = "1" },
                        new MessagePartHeader(){ Name = Constants.EmailHeader.PhoneNumber, Value = "3128675309" },
                    },
                    Parts = new List<MessagePart>()
                    {
                        new MessagePart()
                        {
                            MimeType = Constants.EmailHeader.MimeType_Text,
                            Body = new MessagePartBody()
                            {
                                Data = "VGVzdA=="
                            }
                        }
                    }
                }
            });

            var updater = new MessageUpdater(loggerMock.Object, config, gmailApiMock.Object, memetricsApiMock.Object);
            await updater.GetAndSaveMessages();

            gmailApiMock.Verify(x => x.GetEmails(config.Value.Gmail_Sms_Label, nextPageToken));
            memetricsApiMock.Verify(x => x.SaveMessage(It.IsAny<Objects.MeMetrics.Message>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetAttachments_ShouldGetAttachmentsCorrectly()
        {
            var memetricsApiMock = new Mock<IMeMetricsApi>();
            var gmailApiMock = new Mock<IGmailApi>();
            var loggerMock = new Mock<ILogger>();
            var config = Options.Create(new EnvironmentConfiguration()
            {
                Gmail_History_Refresh_Token = "HistoryToken",
                Gmail_Sms_Label = "SmsLabel",
                Gmail_Sms_Email_Address = "myEmail@address.com"
            });
            var base64Image =
                "iVBORw0KGgoAAAANSUhEUgAAAMAAAADACAYAAABS3GwHAAAgAElEQVR4Xuxdd3xUxfY/Z+7d3RSSQOhVlEAilp8+VISAxi4QwBZUkJZCE0RKAlZiBwKIoCgkoSiWB1aq9YmQgA31vackdFCkhxTSdvfeOb/P3N2ETchm524SxM9j/kt27pQzc6ac+Z7vQbiQLkjgf1gC+D/c9wtdvyABuKAAFybB/7QELijA//TwX+j8BQW4MAf+pyVwQQH+p4f/QucvKMCFOfA/LYELCvA/PfwXOn9BAS7Mgf9pCVxQgP/p4b/Q+b+jArCuXeNUh6MY9+xppAGs1s/VMNK8SYEQXFIpsz12m975kYX2c1X/2fXEKRERxarV2oh27FitAQA/V23ZvWCCLcJmVyrrKwkmnPxy2bmqv77q+dspQMdrY1oFBgR345wFo6Ln5G7e8Nu5GHhalhrg1EsTESioQvjIabdiObkRRy4vr68BMVEOi4juf6kCcBlHKHXa9R8P/LDhqInv/c5Ki0dZdAyNJYadKwphgKWsyJn5d1OCv5UCNO8a0yg0LPBWq2IZCghNifPPnA7Hyr0/fH4IAMjvEZX40P7GhK6KJfBLIgr3yP61066PCnp43h8SRdRnFmzbvU/bRoplMFOgLyCc0jl/K/9U8Rcndmwqrs+Kairr9OtTWwRYcAUA3OTxewHXtdtso1/+b0PXX5/l/50UgHWO7nuLgupjiHAFIFoA6DgRrdQc2vw9320sqk/BeJZFX6eq2t7i5wFwsqtedyLIR6DRSmLa+9jACujZnku63RpmDQgahwxGAmILIHICYo5O+jO7tqzbBAANdiwkANQzpvYlYG8BQpMz7SIncXjDEhE8GW9KFcexv0X62yhARESfUKWV+jhjOAYJQgWKiQA5cPpBA+3R3Vkbvm+oXYCWTumqcbYMEK8F8MRPESHhKiWfJ2DynJJzNOIY0TP2eouizASgaERgYu8jgGICeLU4zz7n0I7PTjVUW2jxqCCn0vgNRHoIAD3nDxHArxbUH8D4uTsaqv76LvdvowCR19wZyYKsy4GgO+IZwROJgeeP5+btXQw7djjqW0CUmsr09sUjCdhcAAg7u3wqIMZvt46c+0N9111jeTExaqQzZLyisBkA0LhyMyIx//Bbh66N2rt1/a8N1RZHxuSrEZSvANFj9a+srZSAP2IJObgcB50740Rd+vp3UQAW2avfvQyVxWLQPRUAiDQCfNdZ7Jiy5+eNJ2oUxqpVCgwa5NexgFZNCtRPq4sIYCgAnrF6nDkGaYi0WE1IG19j3UQIiPV2P+lwRa8mQU3CFzLCQYBQeRwjlwLkc50m7ty65h0vhgEEI5v/7XGmT50NyCYBgnpWfwk4Ir2p2Mon4kMLzz6SEjFAPGeWKhnF+HsoQEQfW1Rr9SWGOAEAqwjeGHiE7Tq3J+7K+uzfNXY6NZVBaqpfgqc3pwZrTrYSAO7yKlBOH1mS0u6p+XdCAP8nXPUyI6MHRDKGbwPQP6osBCIjkTh7v7Hjj6JkOLDpbMtUPSijlpm8ggCH1TK5PlYDWDwOmZl/Vp46jIPMZPYnz99CAS7qdkfr4CDbKkDsVfMqC8eJYExO1icfV78HxKSSuikV/b6UUcaUizRk7wJgj1oEvNnO7YMaJb1yrEoeMeGeAYTUelv1sEt0bB+FseWI2Lym9hDB1tLS8vsObv/sSPXfuy3+0bJ99DVOfyaK+KY4fWJLG9qWA8Kd3sugbTo4RgYkvLKzSp5UYsbf9ScLf7tR5bu/hQJ07tn3BotiWQ0ILbz02smJv5Sbt/eF6veAbmsPB23v37rMn1XYdf4vfRAAFhNAsDeJI9BJnfPR1j9DPkaPnSbma1JPhwBuvwb9nnRV6uza1RoZHpGiMJhRfSeszEdwnGt8UO62td9UbS9h/x8hcO01WOrPzBEHJ0f6lLsVpiwkgDbeZQElnNNky5/BGZ6y6PYjWWD7dqiLAvrTbl/f/B0UAKN69ZuMqDyPiAFeOkQE/IujJWWD8rd/WeiZ5/bPjgZ/fkfLUn8U4MSs+JDGzZq+D4C31ypIAg0QFqnlxU/gw4sq7fDdFpPlkiaAqwdhvVzOL7nk1rCAtkHvAaLXFZiA7ET8mdwt62ZW3Q0JY1adCN40qIVf7wS0bESAk7eYhQBjAMDqY2J9rQIMxITZpyvyxa0i675LgOptMfA1syV/P+8VoONVMY0DQkJfRcQHEODsS6i7o0RwUCd94K6sdVXuAT22/hG4rWd7v57oi159uGlgQHAOINR43Kgm47POvjGpX6unW4dgfa16kd0HXMms+AkidPQ2vgSgE9FHx0pLE6svBq7dsI1/O4DMXaiiUQQny0vLuoY8srDSKCGOX+Ln+pKF5Pz2me28V4CoHgOvQBUyEOha8DB/Vu+ZYQfnfEpu1tp0z5Wvz4Ldto2PdPYLr0NLpl2mKXwzAHq+/tYoVAT4StHK43H0gt8rM6xapcRBHKwehH5ZoKpVxCJ7DRzOEBYgQiOvIyusAoj/dur8oT3ZawVMpDL1WUC2jY+gf7JIn9hSZ7a3CeAWn7OKIJ807SbrmHmVi1HcKlJWw2rw1xrns04/M5zvCoCRPfsPYAxfQ4Zta+8jaUD01o4j2ljYs/HMIH9NKtzk3yXYmZmSCgCPSWz5oml5qPHh6ug56yvbKS7B4uGsHkx/7dr1CAy5uOlcAjaKYQ3mWE/hiHsA6Ym5WevWeS4G4k6yyV9ZpE+NAcbeB4CmPucagoMTf8WWMCfFQxbiEkx1McH6rNePDOe3AohHHz10sgLwFCB6X/VcHSci/i3o+oM5WzccrJSFnwpQvji5k6rgMkLoLStXJBimJs5+qyEUIPK62y9WrAGZwDCm6mt0Da0jKuUET2tHna/uqYfFgFJHBDjbtZiLCONkZQHVTcPiDeCCAkiLz8gY0b1PqGpVX2HAhng++ngrhYj+4DqN2Ll17deVK5+/CpAxtZ+CLBMAWkq3mugp9dAPMzF1k8vsWo87QOde/W5VUXkDETtJtEcjTitO6/aUQ9s8YBF+yqJw3qTwwFDLz4jQQaJuI4txJLSzOBznfg8QCvDMBTOorPyMfGLVY1bbm4AYfdajT40LH50mTsm55YeXwvbthumx66pfrTsGXW7aCmNPn3IPYyxd5vx/ZsWH/5SzsltD4t2XPzHoq1dj3c+9ccqlvcpHA6KwhNUEQagiDderMGwh0pJyszbsqvix66+/Wndcbl4WlD6xpcZsAuUpYwxw78dwBEAbakmc95XxD/EaHxcnjkB+PUiamjgmMp/XRyDD/q9a3gaAdlJ9InAS56+eOHlqxsmd2YYJLmID2fb0NXfxo9Q4q6NdxycYQjIABkrV7cpUSkQTrYlpGRWDHtM8Dv09d1fU26Zbt6CwwLYvAoMxCGiTaQ8R/U66Myl368bPK/J3/Hp/wIGbLjbtu+DImDIUUXkdankLqaFNDiCaqRZpM4WPgHH/+GYTQOpNfj9KyvTbbJ7zWQHUqN6xYxiyOSA96MCB6LNyro3d774HRCzYbdtj0gok4A+6ky0noHurIR59yNdYe1dZE9MecCkAKbBjE9Z10N0v4csI8DYD/SmRCKicODyRm7XmlQp4dMSG3bY9fc1bxBzpKRnIKN6sLBDwg2L76VGNx72eD6lfq9A1hqB+LGISEpDLct4qgHB+aRoesgSR3S896K6tfxd3QsLOb9dkV269JoFwtHhamK7SSgKIlROjRy6iteqh4Hsw1Y2Jrwf8TWR032sURX2HCCJkjoKiNeJlEICvPsFPJZ3Mdu2GhkKanICUmqo625e8iYDiRdxUQoB1ioMn4Ng5x437kDHb6g8XZaoxXjKftwpwSffbOtusgR8h4mVmOkoAheIpfmfWmuX+uko6MlKuY0jvEeDFZup2580hnQZbR6X94se3NX3CInsOGMwUXIQIIabKJPqt3FF2977vvtht6juPzI4lyVeBgksR4GqzZSDQfkIYaYlPqwbLMFtSw+U/bxWgS6/YuxU0QF+hZrrvWvn0xTn786bAoW2mX4DF+Z+3v/hJDvAEgNxxo1r7Sonzx3b9efCNy1NXm758V++rsP83uqTZTCScILv6V5RBREVO3ZmwZ+tGYb83nb5OjVGj214zDhk+B2BuHNyViSPpvAP/Ln+y88K/kjzAe9fPUwWIUSN7hT3PECajB+ZdbgTFOZx+KivXY/1xEqdF05voNv49AUTI1XdWLoGJf+90ofPhJpPmF/hZRuVnza+NadXMFroWGXbzaf+vXpkwCgC9lrOlKBnAbZo10SDD+0sNy0DC+0Hy7nFWE4j2W0C7BhNfbjAvNRNdOivreakA7aNvaxOMAW8gsn6y5/+qPaNjOtDgnZvX/suscGjpY8010gWEQN7kd/aof1JWXpoQOv61PLP1V8/fqfuAm6wWeBcZyr9HuAsxdkPkX5aSY8TBLWfDo3217URmfEhjavoWIA70lbeW30/auf3ys6DidSiwPj89LxUgqke/nqgqS8ye/z22/nIgej4na61ARJrC4WjLp/Yjju8A+bXlG00gol9A00d4YmH8G7QYNbJn6GSmwLOIcubPszcB2uPUnUP3ZG/81mwbaMnkLk6m/BMRrzL7rUf+0wh8uJow56M6lNFgn56PCsAu7RX7ECKbC4jN/Om56x4Aa+ylJSP2VYNH+ypPy0h+gxCT/Dz/VxQvsDDDbYlz3vNVX22/CyRsYEjIGwAYh4hS5s8ayivQQR+/c/M60RZzi8GS5FhS8ANJLJS3rnDi9K41Ke2husiiob497xQgIqKPTW1pfRoZTUI09QjlKSNhD/0vaNqQHBMO4oYDTLuSFYRY58EizuOtSXOW1WXgDPizDTIR/Dj/n9kO7USQVlhGLx3evtYUFFpLT7lPvGXXpQ/iWyT4WDkUdK+ng0xdy6yv788/Bejdp7kFLYsBcEBt+H+fAiA6TkCjc7asXSNrDqVlyZ01jsJ82tNn+T4yEMEKS6hzLA7ymy4Qo6IH3I0KLEDwhYT13hixGxLQh2Vgf8TMPcBgwuMlryDgqLrKAgB+VhndjyPT/DbH1kMbaizi/FOA6Dsus7pw51eaNftV2QIAyoAo1XnE+UoVRKQXSYrV39mudChDmEsykF/fI3KMuN7PmjR3u++sNeTo1s0SFdx6CoLyBEIt+H+fiijcA+DfDqczfs+2jT/LtsWROfn/kNSNgNBa9htv+RCxiHN6xBK6f+X5RpdyvikARvXq24+hKgBwPkFftQ2M4RnF+ZvFp05OPbRjm08TnCC+dYYoCxHZ8BopP0zPAnIC4GuWhNmTTH8KAM0io0Oat2i6EAGGAFZlwjBbHhGc0nQ+avfWteI8L5WcmckvAEFyFSY8qS9rykQ6Ei5TSsum4CM10KX4XW7dPzy/FEA4fTeNeFJB4YRS10E3cBFbuUaJu7atzfUlKin6E1+FVP+9VrqU2guL6NankyXI+hYAXV+XndCohcCpE5+389Tep2XJwyToT8xKwztditmS6jH/eaUAbbrFNGscFCowOLfXddANz0DAQ8T1MTnZ6zb4klnp4kc6WFTbOwAY7Suv/O/0jYrlcVgBj5b/ECJ79r+ZqfgmELSpD1kAwZfOEucQr+RhHm2jpROa6xCwnAj7mmiyr8NYtspg5Pl2DzivFKDztX2vUgPVdXW59HmOAhE4dKLHdmUVLfD1ElqemTJAARCMal7pT8xOBgQ4AYDD1YRZG819G6dE9S4by0CZJ+MIJFM2ER3WyrV+u3/Y4BOjpGVO6wNAS0gWhi7TAIASjpRki097Vy77ucl1PikARkX3H8UYzgPESg7+uojBAEUQfJR3qmh4bbThx18b1yjcFryS6vbiWUNTyQkEr6j5lGqGPNc4/zdvmokM7jMHQfYuLSIoBdJTcrLWvVabTGnBBJsWFPA8AEwAPx/fvJZPsLEg7+T9zactraRLqcv41se3540CdO0a0wjCQ8TjV3xdL32euzkR7eLcMXBn9qdVmco8MrnpT/5bHxaPqoNi+ER+qNiVpErXQIlRi7qubxdmVT8BhlES2WWzaET09qnC8oeP/edzr0zWLl8IXEEA99SX8lU2kOBYma5fHjp67knZRjd0vvNGATr1uCPCqtoy0XBCr0K7XScZCHg0ckrakbXG64NO2atTLlID2I/+vjzX1kBE2qDojnisTptYy0dRPfvfiwoTsqiBjdpPcbh8Jb53kj54d9b6fd5KKVg0tkmjgEYr6/f876oNAfKUUn4tTpiz389e1Ptn54sCYFSPfrejqryO6BcG3/uuKxCRxF9np/Ym7/BCn16ePjVZQXyu3rd816Af1rg+IiBp7heuRtZOlmu8hLdWn0dgE80jYWufHwT0J+gwMid7zZfeYik4l0zuiYq6ujb6Q79nIYGdg/6iLXHuszKy8LseEx+eJwoQo0b1DhkLgM8xxPpb9dyeUQi0icD+UI4XRKSWkSzoT4bX587jMQaccz7cljRHMEy7F0Lv4Zy69IhtqygsHRDv8A8J6330RSwFID4tp+xwegVpQPXcWvrUgcTYh3XEQnlphGGZ+0BNmB1nYo42aNbzQgHadOsfFBbIZgODRASQcvo2IxUC2gM6Dc3JXnsWIlJg3jW1sYh3dZ+ZMs3k1YketR1KW4ipvqM4CiQsU9UMQLjUTB2SeZ2c4A3N4XiyppBSgmi+PHNKgoqKYNdrkIQAGxWt4D4cvcQULqlBGmPawaKBWhHRvU87i9WyDABvru9Vzzh0EBUQh0dzs4vedptDheIbQSucS5NvBMIlANClgbonavpWZcoAjH/JxZUpqMJrpglXInsNHMaQZqOfSNja+uDCBcGXZcjHHNy81nUO92hL0eIpzQJVRcCWa6ahrwcBIcHvxGjYGTfJ+o2fYLaJ58UO0Pn6vt0tFvUdAri4ro8+NQlAvAcQ6HPzCx0vVFpARHivJaNVTQmbhojTa6M/NyvUs/ITFCPAuErWOC+BIjp2jAmwtQtJZYgTa2HC9rs57mAie3Vdj9+VvX6LqyCDvtHAbGqZ0wYRkKB0Med7bKZFRIK28nlVOTHHCC9bD6QBZqqvnvd8UADWpWf/IYrCFiOCGQ4e6X6LlQ+Bf1Raro+vdJNMJUaXJAfqTmUFAa9/k1+V1gkLDH1gSZjjPvuemXSe2dped0vT0IBgQcZVNyRs7duAiOM1MWfLuqWVKFn3LqBlpiwloBENdBdyt0rMePahYke3aZjQ2IvrELZJeiLUkPEvVwBB+hQa2GYeIktqiOOP+wgkwij9l+uQsDN7zY8VKx8tmxSmcau4nParixClvhV0KXrhvTh6iddgGZHd77yS2WxvA9FlDbETGrIwnGJoeWEJPXLGP4CQFo9WnWrYCn/oT6T6XzXTetXB4w26lMpd6K+hS/nLFaDTtbe3t9gCPmQMr/FDkNKfEEAekDY+Z8v6f1ac/wvSp3cLZlw8zVdGPJcu0HRG+pXr2gO2US9XUJZX3kPcRWGXXrF3CT5SGfpD09V7fEAE2x0Id+/d/Ik7wDfh6YzkywIYLgUyQsE2dNrtJMfIoMT5Lu6mvzB22F+vANH9brIy5QPZQXcdZ6hQGCwEVTeiz2glrjXG5RgyKzdvT6pARArCp5J2ZZOtSCLqyblI4ujxaPYfPy67yUWeW1UBuna1RjXt9AQCe1J2JyQCJwCJV9UAAAyT/g4oXycet2vLOoO3c1VcnNLvjksSrchn+0l/Ylp+nGiG9dAPLxpEwt6NAqbLNfvBORp7b80SpK/2x4DBkyY4L08T4SYAOsIQbiXAjnIDb+BDv3EWOwYJRGT+y482Dg61bgaAK0wKoQwIigCpWY1hU7111WWBeatYL5gYPnpJlTBO4hMDCRscugpAgv68kvkN/iDinyJiKwC4GRGlLq9EZEegWTu2rBUPUvrhxaOCmimNF7mDX3uNwnN210hHwBPkip9s6v5GQDss9uJeKGgT/8Jkcuzrt6VdYvo3UzgKcNa9WFMM3hqqI+JZADjTySnPgvQAMCZNGAsAhxy6I04wJAj8T0BAcA4hNBdCkBYE0XaG+E8CepLMk0V5xcR36R17nQLsfURsLyNlMYkJIF3j9DYSb6oqSgoi3iD1rbgHEK0je0lC7vdf5fnrC4EApzWAZxjQYAT8h0zdlXnOCqP015hDpcfdVOckM3fu2fcqVVHFmVdKeCTcHIHPyzmc8xzs2WPv0iP2FkVlSxFRjreeqJSIC0TkIufSSTcAqB/rhI0F3YKMIBDoMAIsLNecKyyqNQ0JbjYFoBPvAbo+EkfPdTnonDEBsqgb+icxMoGEFbEQkCfmbl73OYg4yq2U6cjYdASvgQSrjAoR/Uaa9mDutg3/pWXTO2o6fxcQrpccOnGbPkII39gdfGKQFeOJcCyZix9QSOi4zxI/X8AyPGUh3YT6yCgz7vVRT01lYGR0/zjG2HyU8Dt1R0L/nZNzzM6sDZ+KArt069+MBeFyBOgrYzUhIp0A3svLKxpzeHL35wH5eB1QkVUAIPpU1flQOLK9AC669mKN40IAuMOEgEqR68PVpLkuqkJ35PRm0dEhzTD8FWQ4TGYndNvzP3WSc/ieLRvF4xpG9Yq9EZGJF+32UrIAOklEY3K3rP3ImZF8O6BBf2IGhv4Z13GKtfG+XMjv0ERXmKCTuVdeFi43STVxtqCgEanOUezl6z6T869TgG7dLJcGt52GBNMB0acTijvow2Y7liXt2+wmexWO4wFtklFhz8owSLjix8GP5XY+ZNeoS5/kgMMEgZAQghTpjoeLo7/HBiIYZq0Mo+R6D4iMHtiFMREIEKJrCwR4ZthIAw4v7Cj784UKTM8l0bd2sCnBK4Gol5QCEJURwHPlStHLex667k5CMEtcVeU455cL5Vkuo+f+GPSXKYCL9ClUOH0/IIP/d9mv+fLTJx0ph3ZUhv3Bzr0H9FURV6AkkwMHOBTTPvDZ+THthygMb6wIVyKhAOUA+IQlYdY8Y/F2OY6IS+REMyhSAv01S4lDOIe7AvmtWqVELlh5GzJ8nYH38KeeqxuJ1VujMbkeTu7GLsLCRTiph2S8yFykAfR2txYhTyyPbTMRCKfKr6CGo8/Lql74TAWmx5GRMg6RRCwHE5dh+k7V7IMqI2v+Ba/Cf5kCXNI9trPNqqwEoGtlViwgKNWBUvUjzgWeNCcRvft0tYJF+BFfJVtOXJewrJTuLa6xKixcKIB4iPR1DBJYdmcpvzbQA8tuT0++nyG+DgjSDBZIcEgn3s+WNOc/xoTrM8HWpfjAOAUwVYYJ270T/uzUIHHPtjVnaE7EjhrUdhIACjOqT2uQ+xj13Zirmr388JXN0kye34t1TonWpLRV6MZU0eJpHTSFbzfnU0EFSPSw8tnBf+Lq1eJiLiJqGhitc5X+MgXodEOfm21kFRevFjKdJaITnEPizuw1az2x7O263hHeqGnAEkS4S+YYBAQ8tlNo0fTuzUNCrKoipC2M8qrvi/AJVdO7ooc3kz196kDGjEB6vkOHVnaSnASwwJqQZqy4zWPiGjUl51zGaaTUym0Q3tIH5adgwv5f1xzzkB126dF/oKLia4jYRkKmREDHX+rd5mC/i0OuNkV/QnCaEx9qS5rzSUU9lDEpXEM1BwClxtP1nTCjQqYCONWIKi8exGbMOKehVP8aBYiJUaO04InIlBck7f8iBOoO0nTDauE5uIYDSStVrJ6PygLIurcKgqd6tIT2oVajKEGYWfs9QNwdcJXiLIj3hPHal07pyjhbBgjXmsLPeJx9O107sL0lkFYg4Y1S7xkEDg58TlHp4RcOb99eBVIc2XvAlQxwKSIIq5rPsSUg+6zebaDPJaEmIOiGZ9mvKvIHMH7ujkoFSB0VpLcLFRfhh0zJAuDjUnImhFXQp5/jXcCnkCRWEtNZxPk/KCQkgwDvkTm2GOd/grUlpMX/nrW++sMJi4oeMBQVSENAKUrzto0sMKNHS7iudZBx5xQjKpRAvAB5EUgecn2UmjRXOIqcWctTY1St3TWpgCzZDIEsIv1L4Vqc4Mx3M2ELPFJHGVmAgHYTPZKTFfAOwOoqZLftetwRHqJaFwGy+2R2wzAro+d7t4Ib2oXIzwMiJyAuVP/4flplOFi3RMozku9UEEVfTOyIsFlFx0iMn+9y0zzHr8LyHTc9zb1/cPE1d0YGBlo3AOIlMsWKlYqIZuW6Xy6rf9MlOraXiixd1oncpiBM6dYc7o1sDCpzKYBm0MEJzNzZiYD+palsZNDwWb9X/9X++tTLmYV9AwjhMn0ReRDgCAEMsSTM/iayZ+xwpjBxbJG6PHKifaDpQ3O3rd96dn2CTqVcwLufknkPuL51EMzo2QraNLLINl2c+PNJ026qifq9OH1iSyuzvoOAN8sWiACFwPlwteI45UccM9m6asr3lyhAl579HlAUZbHMpc91VIRTGuMP7fpmbY38OgagLsCWgUYURTmH+gGdQuHx61tCoMrcO4CI4UY1XIaNLf9j1a4k1MTsYDBKBAYLgJt8AAuCYg4wtFPmls9CLc3nE0GSTLsrTMG6xofs2rbuz5oGNKpn7O1MVQS/Ua2rsIUBJF3ZFEZcFg42VcIGdqays+5ClcegN6cGa058CwDuMnMMIk7DrUlpb7p2gFQGXWeg2WB+/irBOVeAdj16BIaozV4AYOOlnb6J/m3H8v57N3/uRi9W7a64B1haqi8BM+Joifusz9SliRVevqkttAs5cw8Q679SfRcg0AjgDYu9+DF8eFFx9YIpMyVEB/iIAG7xWemZDGUioPft7+/87FgZrEIwAlD4HAsD/EaQjqesk3bsqDn+mFgMrLaANchqD2oRqCI8F90abr2okdzTw5m2b1ZLyvrXxPFJy0YEOPUWaYggGKVdgpVJxF9S9aIZlVBxPyPay1RVPY9PoftTaG3fGAMUECi8jm6VufS5gl3QW774bKJ6xQ5GZG/IAsJCrQxe6NUKerdzTQBhDuU1KYBgNNNhtPVw0Ls18fryX38AACAASURBVNtTKjBnu+R4RDTjR8tFPOEH1x1YvyPP/gpKHp8I4DRwfXxO1jrXallDannl7cHhobZXBL9SbbtKiAXhxRvawA3tGpkaYiJ61OLFv5kAUF+SMpgUehUAG5soeJeTl90SlLTwkGsXEC6jYmNueJPoOVeALtH9ejNU0hnDSCkBCfu/rj+2c6vBaOY1wsklPfpeYVPVT2RpVVQEGHF5Exh7VXPjHiAUQAMGKvDqr8KnEeAhNWG2iDNQY9LSpw0kRqaYFDSdDiR/8+fOr/4ojpG0hAk46X5y2O/a+d2nrjeEmpMSFd0/ERl7uTYPu8ua2mBOTFtz53+A6gwXZ7VAy0wZAACZBCAf3YegnAE9riSmvWwUGLdKgbg4OBfHoHOtAEqX3rEjFVBmSa56gs7pd67z+J1baw9416TbrWEtg4JWMkTp4NbRbYLghd6toUmAuP4COIAZRyDXXxWJdhPnD9bG8+9In3w9oroaENpJKTUA5Jfr/MnsI+Vb/igJkNkJXb79sPFoacngfB9hnzr16NfTpqrvAYJXZGlM+2CYf1Nbc8cfgkPA+HBL/ByvwQcd6VO6IVNEZJwrZGXhRsKdoUsRO0BrUGA0evWeky+79pznVAGE03dA+7DnEGm8jJWiEv/jKEvyGey5WzdLl4BWyYqiSuGChFg6hlrhpRtawaXhwp8Ewele+y3GYciYcpwQ3rX8cSAea4n5KyLLawrPBMC7ZcKJipm8I68cZmw9QrtPOUTdMuOgcU4v5Hrgf7wN7cU9+14UoKpvAUGNuCBRWf9OofBcL1OxL0TM349Upo6tZLeooQEizrLevuNCAkgw5S/BaYNaVn5PJUQk9VcrpF4mYiw06MuwjODrS9mgU8/bW1iUwHRE6CdjpwYgjTisKHKUTPvz+698hRzFiOtjYy0WJqKaN5WZVGFWBk9c3xJuvSgEFIaGKVQoQQDo7hspFekaxAWMTvu8NiHQqjjFWXjRw8iYIJX1CUMQF4DPDxTB7O9PQF65RNw610pwkgOO27nlE4HarHVShHfvHtrC2mK+N1xQIwuDOTe2hh5tTZ3/y4jz6RZe9Hptfs1CTmWLU25SFfgITFA7ItFezmiINX7Od657wK9WgMs0L/Qx9TYnz6kCCKdvxWYT0QoF6avvuolKdYCz8D/eet/5+lsvtViD3iGC/5MxKwpTYOIVTWH45eGGOVTcA0pBgUDQjUcxOMtpw7vctaXTBhLnSwHR53uAQ+eQ+Z88WPZbPtgFQNtHcu+Ev2i6Fr97q296cxC4oMA2kwEFLgjPmuU3tg+Gl3q3gWAhANkkqF0YPqTGz6qEP3j71E02nANoItayQZdCM9RS+3xjF1j1qxXyL6OGPgb5noSyAvKdDztH97tbZcoyWfu/gf/RtaSdWzeIC6jPmSJeQhsptnRkOFBmhxGdv/PiRpBybUsID3Sd/ItBBRvoYAUSh+4dpQ6lV+NxM3267TmXTr2ZOFuGEk4hhXYdXvz2KHx+sNjwk/SVKgLd2R0l4/d/95Un/sfbpxUO9uKB7axzzlPXt4R7uoQBkzp5uasgOAaMD67t/F/RmPxljzZupFvF4+CVvvp25nexy+FqC3OMxpHzCwwFKLlMhNQQvt8Nls6ZAhi2+jbqMwg4RSb8kQutSL+RUx9cHf/jTRrGHaNDyAyDWFbSR/XypjaYEd0KOjcOMDipHYDgAAWCjQMRPKNqBS/42vKNzeLVh5tqAcErAGunWBFnmQOFdnhiyxH47ZQLEe0ruYi9+Jz8QvuLtVGbe5bj8razZCDQPzyPg2LAhfm3XydzFKxEtKlc53Ey1OaCcEBrVzoFEESgcjNpTZmmJ7jqIIS0Y0GQ3LK0Ie8B50wBLu5+S8sAa/Aqab9Vl/1/7WnNHn9oWyX+35cwWVTv/sKrKk2WWrBpgAJPXt8SYjo0MlZEcQwqAdWlAFWcV3xVDSDjFCLO/9l/lsBTWUch3y5x/ndTOwLhxJyswnd8RbqpaGXUdbc0RVvQawAsztPK1DHUAs9Gt4b/ayGFvDjTaZPxzrSMlLtMO9kQ5BDACGvi7O+Nilf9EQjQztGQ5tBzpgAR0X2ut6DlY2QoBRkQ+B9OfPbOLetSZeP8CpkJXBBjLIOh3DuDggCjr2wK8VeEg0VxwSLKQAEL6KesqA1X4+et8z31XTmc6SnPAqPpAOgVXOPUOazckQ8Lfz4JEsd/o1xOtB80fCh32yc14H+8to5F3hD7OBM0K4CVaM8b2gXDMz1bGUc+E0nQryywuCHcMt85MyffBKC8D+D7TlRZHoGGRMPUJHcYJeMiHMwg9eIGOwadKwXAyF6xExgqM03QHxbYnc4H925z+f/KJoNo12JZCihemuUOub3bBhmXwhCbeAUw3gNII/zSqpckBo1ecBYAzltb7BlTL2fIRBwAQVNSYyp26PD4liPwzSGvQVqqfOd2XMk67Sx+8NC2f9WI//FWV+fovrepTBW7buWr7K0dGsHTPVtBmM0E+wmIoOPsdmvCrH/LjgOlT2zpROtbiHCrGVwQEiWoiWmCttH1Itz8RBA83OIsCIpsO3zlOycKcEm3W8MCgoLnA9BDMu6PotEE9Eu5pt21f+uGg7464fm7662h8QsINEEWa9S2kWrggiLFe4DLQcZehupzhXh47sWCwFUyUeqIAGfb5rOR4QRvn+w6VQ6Tvv4TDhULNxzfSeB/BI1v+e9FUw8c2CTdFlHyJd1iO9iC2MeIeLX428IQxvxfUxh6WROwGVQAcgmBlijsxESDzFYy0eJRFk0NE/e9J80FHqSPVAsNxWFzXCvEsv0BcLCjo6HMoedEAbr06B/FLCwTiXrIrMoG/odgZWEZH3uGv1JS8sLJ/IYBQxiBiDbj0yYvShXAsMeuawn9I0KNewABlBRwS0KLpOcFUZWEneZM27SM5DhCXFVTa8X5f8O+Inj+22NQJiB2EomIThOHCbnZawTjg6kk4i6EBsGrCGy4uAe0Dlbhxd6t4eoWgTLPJJV1IYd4NWm2eN01lbT0lHuIkSD79WkaPlMw5YEO91pGpX1j/C/1a9dZLfUmuRXDVAtlbPEmC6wpe+ee/fopivoGk4QKiPO/rsPUXdlrFpk5/1fULRROUfFzWZIpsRbe1TkUHuveCqyKwc5RmI+WkS0Tnv/YvAJM7U/APqwp2ryw/8/94QSs2lngO1KGuzNE9Ifm1O/Y/e36HD+GgnXpHRuvAnsNEK3RbYNgXkxbCDADfybQOOBwW+IsAbE2lbSMaf0ISRxnTLlJEuBya8LsRKMygQu6rDn+fRXAcH8MFfGunpa0/wtf1YM68cQK7kpTUgeADlf0ahLUJHw5A+wv9eAGAFc1DzDuAa1dziHf7qOQwVF/PnnQ7NZbnjEpQkGLINytQvYrzJ9/Fjvgma1H4fujZZJdMuK8ri8p4MN+/+9ZnnBSZQijgMKUleJ94uYOjVAc9UymHw9oQaM7j079yeR3QOkp7TSE5YCmoOLCL+MjNSHtHld9hJAKCqTi33MHEHQdzVnTuQg4HCSIbF2hfWGznTtG768ltGmtg9G1qzUyvHOygjBDxtFclCWOB89Ft4JurYKAAX5826E+cZu6xpBZE5zhH0D0VvWYw6JXPxwphee+PQa/n5bEeLn8f2fm5u19QRD6mp2AIn/U9Xd2RNX2plXB6LFXNWUJV5rxVhT7H33yk9J8zHUjk4+Z3Q0FntmZkfwmGn7C8omItlksWhwOf9l16W9AL7EGvwNExPRpZ9HVlQB4g9T5H0DnnJaXFZ5K/v2/WT5fYL2JtUt0bF+VsZWAKEVZIu4Bydc2h4GdwrhFUd5UE2aN9Mc/VcQc05WwdAJ8wBMYp3OCj3YXwPztJ+G0s4KNyOekyHPq2qjd2eur+CL7/MojQ/OuMY2aNg1Z2DbY+tDSOzuobUJMuD8CcCR670+HMv6isS8V+ENZomUmvwoAY0iS+1U0HYFOAVCikjDnY4N2pQH9hBtcATpd3yfaZjHoTwRU2Gd9RFDGOc3Qj1Xl/zEz6CKvwAVZLUHvEuL/yXwr3gMejGoM465qnheswiBL0px/+bPyGMC4og6jGbIXyMMppEzjsPAn1/lfZv5X8P/oxON3Za2TNj+e1Vc3X9Co/2v21KgrwxtZTVh/AKhEcKla9NPp2GQxN7sbirY405N7I8MPTfoHCNeMRWpZ8ROGF55QAHhGuEtKrxwyY+5StoZNSmR0bJLCmIgAL8c7SXRcBxq1c8tawf/jd4ev7t2neYDN9s5pB90qU4gQRK+2wTCxW/OPL21iG+biqfHPM4nSp16pKcpSIOpWId4TJRqkbj0C2YdLfYOaKuMZwIdlpeWPHNz+2ZE6DBN26TXwrkW3tll4Q7tGbSWfRtzV4Q6V6w+iIPHy8xhCr41r5AxolIkAg+T7YNDQfKSUs0SXH7a4BzyDfzsFENtvs/DQVwBhpMzxR6DPCPE3TjRk55Y1tXk9+ZTlqsdGNv9sb/nb24+X3eaQQZwBwMVhVohuFTDq8fS3zrg3+rH9unaBjuLsO9i4xhFB7qlymL75CBwokjv/E4CdkOYVFdPz/piCPQV0d2xcj2d6t868tFmAudCrRGvU0AP34KAK1jZjzZSz33o0QEtPvp8YChSwfCL4QnVaRuLYF1z3AD/GQaayBt0BLrru9ouDAgLXIMDlMo1xox7XlZ9mww/88nGBzDfe8pxelHzjmgOF7y/86USzIofMHgAQrDIBEZ6w6bMPhPula6D9DN+jZaYscTmFABPnf4H/f3bbMSiVtf8DFBLQxFxW9DZsMiLK+J1y5z1yZ8sgy9JGFsWEBwyRQ6cPgkdVBPYzVBn9UYDyjKn9FS+mYa+dIjqJBMPVpLQNdRkHX0JrUAWI6tE/lqn4puxF1Aj6gHxm7uZ1z9Xm/+urU+J3++KUu37JK/3wma1HUXbVdQmDXvvtsHMK7Nnohmr6N+j2zClDGLDFABhsN87/J2FlTr7U8cfVDNqngzps55YPBf7H9KrrKSN/KBydOmlf/168uN/zr48/U5afskifeiVj+E/B4i4zdq48RMgFLmiOINpyJ//qr63OBlSAGLVr75CnCNh02Thegv8HOH9oR3bN/D+ywqPUVOZsVzzuaIm+MHXrUfj2iHxQciLaWu6Ae/Z/V8m7KWRkegLS4inNNEXZIZxC8so0A/7w7xNySAIXEwbf4ihnQ/f+8IlgSjBdf+WUEawV7ZOHIeF8Mx5axU7uGLLuwPwN695/wk2fKiv+s/Idf21co/CARm+RwRdkIiFNUzcemGsQ57q2AQZQvxfhBlOAi7sPaBlgoTcA2QB5p2/6r720tP++7V9KA9BqEmfJwvFtrIFB6+ycXz1/+wn4Z26BNPKSgI6R03l/7raNrqd4PxMtmt7EaeM/AVHHn46VwdRvDsMpGfdH1wXYyYmn60e1yZ5M2P40JW/lhNAwe+CrRPBgTa/T3so8WuLkQ9cf/OKP08XD9m793B3O1J8WABxNmxoc3gRFJCARJ1kehET0HyeV96ukS/Gv+lq/ajAF6NLzzmsVxSo6LckOYDyBvVlYenhcddJXs/12ZE5NQGALiChIYG/E2btcEntMQOWg8Rk5W9el1W3lHRGgt2uRphGNfzsn3zgCyV7GCagEOI3NyVorWNbqlBxLk//BSMQ0gwgzBX2wqwDSvj++q6TUOTj3u/XbzXxbPW8dgHEiiEeyNSFN3MkaJDWUArCoXn3vZ6jOB5SjyxYTjzRKyd26VuB/5DxFvIjE0zFlT74dxn91CI6UyN0jBREvcvro+Im8+JM7s0/XReplS5NvLLPz91/89lizDfvliyLiBx1lPHbvj+t/rUv94lt/HFPyyzV4MusoZB0qztc4H7sre93qupikRTucGcnRAChg6l1M9cmkI46pshvqHcBwf2xlEYEahGO2T/u/G/N+UON60u6s9a6gaX4mWpYaoPPSFeS2Owv/2yezjsBmSfy92xT7i5PgoT1bPqmk//anObR0QvPvjtI78348fuuvJ+XcH0U9jW0sS+NFd33/lU8mjFqbJVjr9LYpw4mBC18vmb49XAJPZx+Fo6XOcgSYVVBCs+tqihUhpXQnriAwE0fMOA9uUJXge3FkqtwFSrKPFdkaZAcwaLoV2+vA8F4Z53R3ALxv7I7SUT75f3x00EXMxJYCoOGQ7dQJlv2aB6//kif9qkYARzXOk3ZnrV1fl2OQOPuu+zN/5cqcgruOl8ntQOJF+u7OYbnXNQvuGTuzbjF0jUmn4btEBihQOv3r99MwI/soFDq4TkSr7Y6SRyWd8WutQ8ZltHoBBLQbfBCTSXeshowNogCR19wZyQIFTTaIyCM+6zD4/zlfXlLIk/1FPYq+VfDzMMaeJzc/j8DgbzlUYuwCsu8BIhwrcf1Jdmrfqzv8BKGJ9rw5dWjwt0e0pduOlN9n17jU5S/MxuDx7i2Lbmsf4pOPyNfAOxen3ASqEfxO2gNe4wTv5ebDqz+fhFInF+EqttvtjoT9tdMx+mqK8bszPWUKMBDcSS7PI4mEIKAxcnxEEsWdlcXn5PSjUOwSHdtHUdgKBBTQQ991EJRyhBnaYcfCulg9DCCa2ngJADxArngXwqICewvshhviznzpY4imE19eojmmmXDIP0tU3bv3CdWDbW+UaiRgAFI+iFHhNpjRs6V+aXjACmtimnhI8zs5M1NeAaCHzTC0CcySgGx/uv+0sL2KzfmkptPY3VvXCkCe3+ZY0YnyxcmdFBUF8ZUZSCpHgHcVDR/G0bMK/RaGlw99T06zNRrhT1tPQ1KeloUiu/h/KGnn1rrhf7yFLhWXume3HoWv/yiRGkHXkYw2k10flfv9hl1mRVCR34AiW61vAmG0jClYDMbNHRrBY91bQPMA9SNLUgUm3r8W+HPkKHVyY7H4+g+3Gy6BA4DP3LHl8PMA2+VwHF6a6xdhlqusKiFZ/ZNGzV/VuwJ06NWvSRBTVzAA6XOnZ9TyunSufNHESMVqfQ+wKje+eIld9MtJeCenQN4USXAQuDMpJ3uDcHL3KwkmbJczilwke8FYPeyycCNwRZCFfa85HPcHjp1/wJ/Ky15/tKNqsywDwhgz3+8rsMO0zUdgl8duKTw5T+eVD/UIT2umyMq8tDg1SFcNA8V9pgog+kVFfQQmzPMfFXuudoDI7gOuZFZYKzvoxqsn0toSro+sIf6XKTm5XfDEVl0lOIPA4qzdWwjztp+AQrscLkiYZblOj+3MXiuiwftjllW69Oo/WmE4T5b+XHB2TruuBfS9JBRUxHxEHKImzKoxKo4vwWiZ0/oQGO6IXhkqqpchzv/C/j/7h+PCD/JMEm6ZxPvXCZbtOj8hZEyL1ZHeJAAz8QMcLlhE2j999dvs7/W+A0TeOHAk47AAEaSYV4X/LwLM3LF5Td3xP+kp9zAGgjy2ShInGvEaK862ByW9sdzAvFX5heWJsmxsnpW26dYtKDSo7RIEGCyFhAWANm6n9auE0zqgEUbJljj7Y7ODKvLbM1LuYkCZMlylFeUXOXQY8/kf8FveWXelYk765Nwt68wEAamx2fTW462dDue7CHijmX4h0HA1wR1GycyHPvLWqwKI6CRNwgJmMoAx0vQnxE8Bp6E52etcqD8/E82bFOgMU+cjoAjPc1Y6UuyA1Oyj8K20Py4Acf6rQ3fcvXfbZ3vMNiuiZ59Oqmr5iIHsSzjAP1oEwks3tIZWwYbXlh04f0FVT6aZoSMRHxovryx0OjA2HQB8vsNU9E3cle76eD8UVN8liTQOfFlR6ZFH6/pK734PcOOCfFsIK9qGCCuOOwvGtRm9RB7YJTFo9aoAkdf1vxitAvMBMbKrHhH/j6PcHrv3h5rjf0n0wchCmSnXaADCiabGLV/cA17ZfgLezTXByABQqHN95K6sdcKUaCpF9uw7kKmqsIRJmSCFjfTeLmGQcl0LcHltCQskfK2gM6kyhKhkC+j1SW11qyqOGTeZIaX674kyGPPFISiu5rLmNgp8W+Z0Dj7w7ad+3Ukqmv7HvEmBrcMs8whIxA8w4Z9Jx1VBejxy9reSYpDKVq8K0KVH7C1MZUuYbPhT4f8CuCKvsGy8P8cMzx46M5JnAeBkb4AvFydPITy/7TiUSeKCQFhASH91R9a6FHP3gBg1qlfIc4g4BVFukAMVhMe7t4D+EWGVTycIVATEh6uJc00dg5xLp98KpH8AgKFSs8Cdad6Px+Gt3/JrfjAk+kPn+vCd2eu/NlNm9byC8tiZOWUYgiIcpaQWB3cZGgItVhPSPODZdWmJ69t6VIA4JbKnfRRT4AWUdEQHEf+L9Md2Zp2J/yUuSoYjtMnkyEh5ExGG1vbZzlPl8Oi/DsHhErk7rbgHINAXZQ4Y7gGP9tmyi7rd0TooyPY6APaXMX+KAsX5f/7NbSAy3E1aS+IWanAUnQkh6rNmVwa7l7tQbZ+Le9ITWUdg/T4XZknQg3lGTTYIuoim5pYdXgbb62YOFXHECAx4hpn3gGp0KZLC8JGt3hTAOP83tr2IgKMlrR41xv8iAIYmfYGLX0tuZQug5QDsjtr6W2TXDBv3lj+lj5Gijbs1cg7bnbXBFblEIkX0jO1hMeIgG55wUjLu2SYIZt3QGkJtbtJatwIwhJlMK3xahqJdNE2c/7kl7DFO+IxEUyuznCwVALgjsM3wnXDFS3YHinLlERSNQK85Hc4Ze77bWGSm7Op5HUundgeOIk6EORdNpC32MhjU6OG0o3Wp3/NbqcGRqazjtTGtAgJCl7uCVfvGfNcU/0tMfrffnakdQLjcqai8TkC1sj4JZrYVv56CRWZwQUQFRPzh3Kx1wgQns3WwyN79BjNQXpalaBedTroyHBKvbOo+/7ufsIX2IO5xoHZzUPy8GmMkVx+b0hWT2to0y1ccQC4Kp9tn+Zs/iuG5bcfgpOGz4Fr7q+0AYnPe6CgvG1Pn+9q8SYF6mLqIAMWOLfVC7lJCOqkDxdsS56zz55RQ0zyuNwWI6H3HPyxofQ8BI+QuXiL+F60ocpRWxv+iUWDBJWDqtZEWTLA5ggKeZwATAM/QgNfUWXEPEAM9Y+tRE+8BYCedzyrmJ2ce2rbNN6VbRB/bpa3UpxDZJEA5C0xjF/4HbusY4oraIrjxiAMyJtxwywHoKUti2hyZhUjPTH6YAwpfBukAAHadw6s/nYR3c/MNyhYXSbx4oPGAL7lWrF2k85G5W9dtk2lLbXnKl0wRcJn3zN1TyMkJXrHmUyomu8lz69iQ+lIA1qVn/yGKyl5DiSBxbm0u5YiV+B/jkSSuqwVXm2NAc8EfUDiO3CWjeAIXNH3zYdiVL0e05gLq0Qdl9qKJB37Y5HPrbdMtplloUNgbiHCXDBJWyCKyiRWeiW4FlzY9c/43AuQwsTgKbYAPLYlpUq+njoypyxFxmIwsKuZOVfgDGUuyUIEqRyDXTlEEnCbnZK8VRLlyL4peJqiny6j8HBY6CB+rdiXBRZdS91QvCtCuR4/ARkoLwf0/Xub44zpS0kmuaYkV8b9WxYESdwIQNwl2cvlEGZPCNVDfBsQ7Zb4qKBf8PEdhkylcEPys6VqCTIC6iO59ulps1reBSCpQXwX+R1iAmgW5rYJcHEMQQOwArq1/g6qcuNfXewAtGxHg5C1WmOPgASgo1+GxLYdh62HX+V8ogOusV3V6uFi76fXT+olkqd2wlgGhZY821nTrd6YdZIg+VXVnPI6eXxeupMqW1YsCdLy2b6vAAFXw0HeXmYQ1xf8SzhuGyFPNrSxuTyMRpFqK8qNc4/C6WVwQ0EnStDG5Wzf4QkTipT379QXD/i9n4RBEzcO6ivN/OARbFBd81Vh/BQOJm6cCYB8gH1wZQtSLkMXlEgmFL0RXmXGoyPPTsVLDZzmvzPP8f7YCuHSRf19Wrg888MMGn7thbW1wuUmGPg7ARAQgM+kIcH24JWmu3xgtz8rqRQGiesXGIDIRiaS5TE8ISAcS8b8cCRVwY38vwGZd/ipxQT+eEA4fMs0Vwy7uK7NyTu19tlaS2q5drVHhlzzGmCKCQkjFIKqC/xH2rwoFQM/zt1yIUjOhWj07/tXB0zB502H3vyouwN4UwEDuPrBz61qv0eIlheqXu6ZLCWGYNXF2nf2la+6hbOvP5FMu7R07GQCfRWRSjg4C/wPEZ+d4xP/yx/xJcXEKv6PDaI6KtNO0OFtvP1Za8sQW4fKnBct01+2x9gXZiwfn1hKwW3jChaq2twngDtmXcMFK/VLv1mDgf9wXYEMJKo4/xohDKYE+7uPPfl85qJIipGrLU1OBPd5uylAE9gqg3OuzKEEsCO/vzKcXvz9hLIZGeJBqFiDPmgR3KwF/PnfLWhEBUnYFqVHMzqVTbwVC0w92ADxFDTk4z2Csq2Oq8w5gwJ+BvYqM3S976QOAfE7ag7lb1n9W0X5/HsCM8z9T1gKxnvJyIPq90PHTyM/+YCdKtatkJyoR/e7UnHft2bbxZ291idCkFkX9BCThz6Kcq5sHwKwb20CLYIugIj9TdBWYDOkIuFTRMNmbU4hwv2wWzl4moBFmIAanHbpz3JeH8D8nyqV2LLdR4BN7WWn8vu1f1slBRUA2nBZVxC640cylHYC+VzXeD41wqnVLdVYAEf2d2ayZCFglIERtzfI3/lf1Mu2ZKUMYkMG+ZkIM9sJy58t9PtgfVuykRNk4YkRUyjmfsjN73WIvnlHCEy5BYewVlCACEO0V/r8C/zP1mhZgE5cBrwpgrMm5hDTC2z3A7hf7GkB+mb62z4d7O5VpJH1vECBBJ8EDe7LX/mZC7mdlNe4BSthjgDjNDGjPtSPSBGtFML06NKKuCoCX9oq9CxhbiIBSoUfc+P+VpwrKx9UV/6NlJKcTCv5NeVQhEBRzgpFXv5kbQAAikrokXoY0TrSyqPTwwzUhIg34c2CbeYiYgIhSq6mISTD9uhYw0AP/43UsjVBFfLgtR5U8/QAAIABJREFUcU6NoYq0jGn9CajG0Ey1zY/jpY5Hblt94CoiGiFrweMExwl4nRm8Rbu0zOR7CVC4sZqJIyZQZP+0Js5+sA5z3/i0bgrQrZslKqDdFGT0uGxAOoP/R4fk3Ow1r0u+rNbYR+EAr5++WHgXDTElBBIIT3roiqU79qoWdSMiXCT1vcFcTd9qnD+0O2v9Ptc3ArBpiJEM+LOiZjCBc5cgAhBftQhSYNGt7aFzk8owvrU2hThPtCbNyazMJOJnrR5knIPt6cn3M/MMzFqpU4+/9q3dAYoKYhGTaojYDQVPofOIRwwHj7ZIydOdqXxJcqyqYCaZiiPmilyjhB6498w9wD/axDopQET3PqEWq2UBAg6W9v8FOKCT7nf8rwrhurZ8Jh5k/mFG4AD0g8qUQRe/8m1BQGjoCgY4wMT3h5xcG7k7a/1XlccgF3s0RfSMvc2qsNdBEgkr6uzVNsiI2hhWgf/x1RCiT1QrDakMIWqAaZEEGYCmhC0HF/WgdCKCf4NTG3nFO3uCGODbsouBuAcQ8TdP20uT/6w0CpxZDKQbIIS4bHpHjevLwaSDDImo8sQfsInYBZWLkXnq9jopgMD/MxsK9l7J8KfikIvf2Mk5el+W/87mLvqTjqMQ8SWTkFpxzv5EPRR8D65apUY265yskHwcMXF8IqCUnNI/MyoRkQZv/SC8tFfZGEAmkLBSEF/h/yui0wv/XxNRW46W5etXhE6tuPy5Jl1ZekovC+KHhNDMzOQDgDVlmp5w5bLfGgVarCsAsJfMMcjtH5DNOSbuzF6zs6YdSbYdgshYb1eygkzGEUOAQo340IDEOcIHpMpuLFu3sXebyVw9b+eefW9QFcu76HqE8lmW24KwvKSO8b9c9CdhS4hQEL5K8e24ZAQcEd5VE2YbQdtcccSUtwBBxBHz2X4DEUm0iBM8tTN7jQs3HEdKm32jbaFBR2chYhICyBwjKMzK4OkeLeGWi0JkDVGitjxnuXZ10MNuYJzYAZ4B1NqmzCcG48zQn7jCdtBqC9NGt0j7RWsaHrKAIQ6V8+QzFrI/HZqesGfrus/PKAApsBpNmyYdmcmLkTDR1FgCFCOHkWrS7PcrFSAV0GxUT9+D7lVDYtTI6JDxTMGXEFDO/i9syJw/rR3T6sb/89q4RpotWATAG2hGgRGpAIgNrnA0F3HEVGvQO0AgBVtw+wl/QfbysTu//3x/heA73jnyosDiU0sA8RbZFTQyPIA/H90KI5sGVOAdfOogApQT8WfVxDkzK9CQMalfq5+32yCgxaYiMQJQGSN4jB068Dqu3UeXBraZjMieBFlfbqJy4PzpnOzilwHcATyMmL47yGwoI/EegMBWERkLkWxyAtGrqr3kaSOOmEijfrTAkmtMgSn9VoCIiD6hllaWNwDhAeklTMT/0mlUnfl/MiZFOEF9DxErY3DJSA2JPlV09kCFLb1LTP9mTGfSwDX3g9hu3a4l7vp+/ZaKOjvdfF9Pm+ZYQQSdZGQhFCkq3PrbwlvahbYMtlx0xv/H13AYK2+2yuFBTJot4gbA4YwnLmqGjqUIeLOMDDzy7CInDLGOmf2j0Lyo6H53I7IFyJikNc8A6b1/zHks8dR337n9A/yL5UUrJ4Rq5QFiQZOm0nHbjH/kZMCjXSTCMV+rsMlcRHlfEvcq087X9LvEEqh8CoidpQTvsqL8Rk59cO62Df+V+sZLJi1jaj9A9gHJHTcqSyHiYyyJcwRTg/HiJEh81VZqKjI2EaXhw1Sic/2RnVnrxQVclMMiew8YrCCK9wEpB3QR/8uiwIJvBnXu0sjKBlba/6WsuXQKgT+kJsw16FJc9Cdc4H+k6U9cx0Faq+qF91Y42nTt2fcqUiwZCPQPWSsWEe3WyvQ7d/9YYRXzb1SFVuuZKYMJwCMajO+y0CAOIBFGyZMuxeXKIJn8VoDIXgPiFAZLwCMUaG11uqKe0NqSAn1kBf+nP6+/xqBnJMcR4irJPrqzkY5Ew1S3Hd1dN0ZFDxiKDObIOq8Y9xiipYWl9KhgTBb2/7CgtnMA2WgEEc7TdyISRzF65MfhkbdYGQ6r9DyRUQCCYlfsrNlG7GAtPeUeYpQOgKbs6EiwSk1Me6CitV173BFOqnURAIuTOca5vysgorE5W9Z4BMDzL4yRy02SPjR3jwFADvFq0myxGLmSSXOsfwoQ0ccW2UZ9hhFOQsQqJFTeht+I/0X8pdysdYIc1bgo+aMAlDoiwNmuRZqAXvueamdyIMAeRTz2JKZle9Ydef2AaGbBdESQcs8zwngg/OiE8nv3bv78j07X3t7eGhgoLmLXCvymTJs48X1OjQ/NSejaFRFfASLXziGlAGTnnJ6xltvngc3OHRg6lSlMhDEy8RpOZcQh2ZrkCjwhxuEmAOVo9IBkVGCG7HuAIA0g4ItyjmjTz8RUczuTyQjCI49jcfI/mGo+kAcRZViUExMqoeImgxpKDVj1vhhO38E2AX8QGHypMojglBH+NGvNpxVCN8bcxHYl8rvoD21fy8KfK9tubPk8XuBHDOcbd90R3fu0s1osGYRyrpzutp9y6Np9e7PXfx3Ru/+NFmAfIsq9ZFbE/9I1GvLzoAhuDbX+G4hcKFoZBTDOS/ipqjqTwMLsWpnyJgDdKflxhTjynI6i/wsa+4YRglQQCSOAHtEz9naLyt5xkxpLTGHhtwab7PaSB+tKn04LJoRqQWbvAUbjj3FNv9k2Zq5fsRykJm91SUT2HhDNQH7VdHu4/tuu6wP3Za8z4n/5s/qL75zpKcJz4LHq9Ie1j5brwqYqzlE4cn6BZ92ue4DlRUScYAIXZCeCl3JP7XkpqmmnqYj4tOyqKeJ/EdBi7Yhz6vZxUcHBjS0/E+cdXJNfejjydOLDFYISYEzsPibZFeCQUlZyFY5/Lc9zMTB2s4CATxDxaonZb2Qhgr2k60Pr6iZJaVODtSa4DFDwhkqtBK4mEglr0HxL0hxBXWM6SUvco2QW1bP/MFRwtiz+X5icOdCbRaUwriLSyI+jwHKNSf9f0QYtI2U5IZly+TNY1oheVJUTs8VWWbHiVfQpqlfsYESDxUGSzlG4ScJau8M5zWZTnxHOnBVnV18CJYBinfNxu7LWvmWw2YUoCwFB4IdMKIAIIcqGAVIxoXFu9lVtlYlBRCstodooHPRyGaWCiqkuL7w2bboFhV3SZgEyjJcukyifiCbmZJ1+t9Ic6kc8Yb+BcYbLKH5kSZx9r+nZb0LilWV37BgTENAuZAYyfFTa/g9UrnM+dVfWujfE+V+sOnv6gLXzRpAm7BcNOLV4VFio0ng5oclwm8SPAKmDLUkzNxmLRgyonq6XhhujVd2AyORwQa4d7FcEfSYRCvv5PyrMDr5mogGrdjj77Pluo7Fll78x8TZFtawCZGbIYgE4f5IQSxFxnrmBpwLkNEJNmvOJ+G53H7B5jAPr0js2QUED3CjzoCcE4SDiaYVl8OKZMErCGICmfQWc6dNjEPWlhHixqT5x+vI0L7wvfPQS0/BsX+N1VjvaxMQ0C9PD0gGhvwz+301/ckDXeNKubesEhgbE6t9tCehm+X+cmSk3CUIlBOhoSkAEX6ilZffhIwuLhKvJphhgN3n4Hnfo1atJEIYvR4D+MnZ89xEuD4hvQMTbAbClTHvcEIL1JQX5w37/b5bh1H1q5tAOIc1bLQOTdnwk+J2jIO4yKQugf2nE4wMT5x6syQ+7S/SdvRRmWwlAHWRk4fYT/ri0zD7+4PbP6uSn674HrABEc/GECY4AwhBLwmzTrHWmFSAiuv9lFmTvAdJlcgJy43884n9VX4FlJo9x/Fk6bSBxvtQM47FRtkekQePMmwpYxffYQLW2SmZMeUYOCmAACewEcAAROiCgHAWJEf+XXswt+/OFCiwRvTyisRbaQpjxzA26rNDOzlcZbMLww041bh6Vq/Ul0bEdbIwJd8PesuNLAP9xkp6wN6tu4VSNMc5MFoH0hpnqnjANM3xIjZ9l7GpmklkFwMje/QcwMFi9pJ6thd2cc768rDA/uWLVq34Gl2mwcH903tYhCRUmOEAlMfxGyQ4AmmNJSBOmQhdHfQ3Wpy7R/fsojAnvJCl7usuxHx1AYJG1mxNAns71JE+yXUobGqyHt55NQElmPLlkZFZDHg0BluQDTG+eMPt0TbJo3jWmUXh4yAJFGhdkyDRP1/WHd2WvE28z0o9QNfXBnpH8JEN8yqSRo4x0PtlyOCQDU1NNsYqYUwBh/2+lPqUwJjx4pJw+hPcO53yGJ/7HHwuQcH/UmfoGEdxj5rEEAU7pTr13hZnMW92dr+93qWpV3gYCKTdJNyxChFISC6VPObrz/6TpzsQKepWK3cjZLmUIIi2QfVT0c/KLz0qIYKzlUNDbmJrKa5RFt26WyKC2kxigCHMbIlmXIA2Yl3PU+bTHe4Dkp1WzlS9KiVRtkE1mLFsC5Mjog/IyeMQsbaLPgfNsXtvrbmkaYg1ezZh4N5FOx3SNj64r/qc0c2oPCzCxwrSTrtm15J8sLy3rGvLIwhO1fRd13S1NwdZIWIKkCa2Mpc7YCHyL0X1WFgRbj1Qn2PL7aGdKEIYsZI4K2KVX7F0KsIXI5Lz8XGLgm4pOHLvvz9zv88w2yzN/HeKIHdJIGxGYOM+4Z8om3yPnUVJU937d0KZ8Iuv+aMiFaAdp2oN1x/+k3EVohPw0lZDgCyUP7sVps2sN1e4fLsgFKpZRAGGK5cTnFpUefqG6S6UjM+UaBWApB7jCVOdMZiaCPYDwoDXBAMB5TZG9B1zJEIWxQTgbSc0RIjps59rAfdkbai3bV5OPvzauUbgt+J+E2NdX3uq/+0OXItU5d0Xs0t4DRgNCGko6obtxM+s88T9mO2WsLqmpjLcvHcUBhBulmXSaSJ9gOTT3LQnCLWbgghSYg4DSjiUuEnEJMRIUaMQf2ZUV8A5AVToPweim8+aziXCst/gGZjpdY14CTURZUdjx8b4Y5oxA5wIXhOw+GUufMUZAJUT0WO6WtQJeYdoEWtFmcTF3tEu+lyEK10/ZI5jrcw5T1c/3z0cv1DE1yUVi5FyfictRs/DQ+YQwgiFKMfq68D/wUm7Wmkr8jz8DmbdgQmhYcOAaAkGfYSIRZesajAwYk7Zb5qvI3n2iFbAKE68ULsg98EbRvpSAOIkX02G529ZvraktjvTJ1yMqGwHR3HuATMeMhlIhkNbfkvRyJYy7lk/VqN4DkhHgaUQ5Xw+DPIzgvcJSGn3mPUC2cVXz0WuT22s25V1AjDZZwtbTWkFfM+8B0grQqceACKsKwvrTS7ZRRJSncz5sVx3jf9nTJw1kTH0LAM2sCMJh9uP/b+/Kw6Oqsvw5971XVQkkEbF1FLdmSZBpte04MDFpvrQrEsC2bVQGkEBCVGQYlSTix7SitluCimijZmERl5HWT8QgzrT2oGFx4XPraUkYQNx63BpiYlLbe/fMd19VhSSm6p1XlfyR76v377vLuefec9dzfj/Nh2U4mwekqlwBDJ+vHgEu5hxsu2eto+6c/U++NrAyvGmZcva+3U22/03fT+19MzOGfew2OJzbFwpaPNAVcDwLxcobX3DZJcKgp4C/GipI6z0hKzxr/65tB9hy9ZMwwiOmbSQgdTXMHqM24geYC7zlD/6RWz+3cMwtnD5FE+IxbuB0ZNKRH4VDZsn+t7fZwRvJfqGGqsdVuKHyveeXQRZJXG/4/TerBzBWvrFjvRNOnHAPASzhQptEyk38Dqz8f4Bk/d7DB26KB61I91XkmD/JeQ1c4Cux2hRNREQfGB3hX+FNq9o4+ZSToOE1XkbozbmcKC8BfUlhq6xl91YVJpn0dWjERcR4BBHmudwSSpL0rGdhLTs6jmcAxcX6GWb2Der5CJHJ72r7/+CGIynyf0WCJSo3EIiE9Ef9dEwXEf2rkX1ogxsIvXEFU2fpulHH9QtiGQAoLCK6vrV5S9yAD/ud45LTFyPiKs4AdZtGgrXck/XZ/VxdnJQ/PTM7A1cJhHLuKZ9s0ADr1pa/H6xLiKHqIHw0UL6UIm4eLJCB7iKJNuvltb/hehmzDEDRHx2b46sFUKBPvUmo47aFqMsi2Yv/y22n2XNr3c25pqYp2IwCl/k7pJRzvVGfF27e0QVTz/Tp+ouAOIabp/dk92OVSqKDwVDo8k/efjUK4dF/yRGgX/fObc5yKuc5RTS90k3ElTa+cHq5EOJBLtEHkH0OeCLo71qeKmxisL7yMiGEOgi78nQFxSovrauw4sF9znph7q/Ucqh7DEX5Wcx89VSb3k+lKctSRRFWwEmaBo0AeDynQbE0CPQpEcw2ogEw3Lwj8i/MOSEjY6NAnMad+RIZgB1AA/DK111ds484YGmGG5ZOBtSUezMLZZvbJgD4FsGaFwuj5OYbU1BynkfXnkXEUzh78ajf1+smWdceBQ/j1tY7XRT2fq1b/gAEajMtmOurqG3i1MxaAcYVTZ2kC+NZIDqdcziM8X+FzGB5MiTTPQVX0HkAWEeuoPNAItHLWrBzTjdiAEcbKk1+vpHrO3Gppmm/514BJjSACIjUnS1df7vXiV2x47Hrjvd5stcnEeCSoHXRABrpr8CFj7g6i43Ov/BUb2amK78gBDwoQZa1NL/8Blfl/aWjPywaHvYNq0PAq1yd/dRjH1KpXlb7Aqd+jgFoeUUl8zShPwzA85ePXInR+g4zdEsM/58jTN80Ef4v7+1C8W0BsKBXVBlI8AMQlfcJluaKgOMKppfoOq6zSS6Yr1z9HoPtyR+/VX4yrbuaVIckPBgqn/iQll0p0G2IY8LxH5RAd3i6gg/ikkdcuZ8fO2lS9gnGCYrsby4b+c++iaGq1uYt9alAX6oWmXWVl4MmFPylm9u/kHpw9Fjtv0cGq7yjAURAX0c9hAgLObO/3RUKSRnpdvNvKeL/2LB58mkAKmAHaETGwnd6gM7EJOk0bb8gQ3sagOcXZDc5OrZ7vgdE3Z/fMy2r2//HyQojHL9ug9wTGYANBjzPGw2id6q/1/8k/ILsLR/R+nY/LE71PeCHVf92gjfLqxBEXGwJ7Ulnt25aZXjtAy1O7XU0gJPPnjZqeLbYKhDPdios9p9AsYhY5a07tyrYuqSvw8INVVMAQUU88dyNowIgwCGNwvlY/tBhrsw906mX0OG6T90EufALijSztwEonjl6IdQWWHzgo//6hiOLzZ4otEbXMc/xCic6bEk5x1cRgVFx+UX8glA8iogncfMS0V86zB8u/WL3n/t982CXs+b6EWFv1h4EGM3NE0lHfkUUqC+IocbFz+1oAOMLSi4BQ9sogEd/ZHv/DBD+T7Cu6gqhYRT6zoUKiB4+1BW4ZZzLJT9Wg+0XdKKh0BFuROTSjUbWgD4GEAKUtYfbgvdyoeCpvvpkS9B6ArzARYvjJyXarRNeGQPScltm3qQZZwkvNCJBPn87SN+BJeftTfEBlDbN9Jjtp90JaHsfu/qIoNRTXrPBKVNiA8jPN87wjVoGApajAwdvj9nf5v/qJLngsx1bk6aypE0rPGZ7512A6DrYmSTN8yysfdKp8Qn+235BIKBWMHnP7HknWmBMqQTUZklasq9XvKyzVEkFhcQtll40ymp/41xr/ylsD2DvsEcRYSYC3wVGAtS0dn15l9PB30kudQ4gTdgYSC6/1Xqnv9rp3JPQAKLuz3WI+Gvm9afaCyuvx5rW5qbbU9n+BNbeOFqXnv8mhFNdNZygC5Gu4d4CxCtbIV9oBA0gcDy3/r4GIIk+AdOaE8//J1654YYq9Ri2GJg+VwmmfwsJ1unlteoVPdlP5BVNv1UI/B03TjhCgiJf7QiH5qZyCaIEDjRUTdEAX2C/RRydhL4ww4HzfdetTugHltAAJhTMOAcM+w6eDZMBQEdChLP2N7/Uzf+VjOathupbJNIdwA3OtiuxAfN3apZZxn0IiSeb7Qrg0VXbL2If/nsUFkXC2/GD9M/6fOefYhSMLFWEn1haBLrYDIDuHoF+VDod1qV2BUbBAFiV95NoXOHUiwyhPwfMKMAoDM5e0wzP5nArJ5LL37D0NAO1pwio0OVFSEgA3KuV1SSkYU1kACKvcPpvhRCrovDnLP0p/q+gJbvxf1iZ+kmU5DZAhSjW6tnhuxXkR7J1q3wR9ItshffPxguyTTBWaQRKvc78Krx0//5trq4fFfx7WMupR8R/SaUNAPC8bsjSbkKNJAuLvgcoHmj2RGgToZO8vrW5SW1fknePtrfCXbehgJuJuOexaEN7xILHa3p8A1CB4pmjKhFhOdv/n0AS0MYj3wdu4B76+hMsgg6QsQHcwp8ozHiE+ZzTP2cs5E2eMVsQrOHziPW88qJ2smBJy84tjgexvrKoe7xQfdXVQmC/fGAc2W1jtGSFUbGygesXE69c2xXmGN/DQDjfxVY4AJJWhL8yV7mdAPrKYa6t/i0QPOHyMbQXGIJrAzi5oCBjuHbcQwLFAv4jCCnQqaqW5tT4v8Lrqn6JEtcSwFhuZ0fTfY9Ic/QFvGdwp7IjeEGeV9x4wHYvAASfhvzmtAN7tkagu11+Zl31DNLcg8UerUaBAYtr9PL7UzKiaHki97xpZZouFB0W80GSTEV+Jy1a1k0m4lIHseTm2qppRLjW3XuAvRGLsAGtWBF3BYq7AkTuwr3rEHAa3+rhkBW2ymP4P0m2N3n4E4D3dUFX4XxeAIyTfKf/vPgYX1b2BoGueMTsYoloq7+jfc6hD7az3I/7yhJsqPwZIj6HgGz60p5lENDHYMlST8UD7zq1k/M/AiIMzyCTAzmKmrE5ZPqvO7CL9wYSTw5aVzXOlLgeAFzwQdv70Y90gqtxYc1e1yuAfQg0jGcAoYhzCIyiHrwRDIavPfhO8vxfStAkg8RDQHK1/p+fLnMTEpew8ydM8EwYOaYSQCjyHYMzUCKDX6Gl0b098X+4ebtXkKRII7pzR4i1RahaYaG6rbu/9GecN/U00PQn3fGIwZum31qQMn/AzJmaNSV5RtBEjnFxV4DRRVNzvagrT0AuC6NJJNd3tsnqGP5/sopPygAIjkiQk7vZQpKtvE8+xSOmCW0jMnnEFE4KgIqEw4p9O15yHcTfPYRVVJQp1hGRO7DYSAGdJGmR8eWwpxIt/25UpEJiR47MWi1AzOFOBgS0J2Ra8w/sSm4b2FO+ZDmhnQCz4hqAjQAn8DlE/EeOogjATyBvS9X/J+kVgAl/wmlLzzS5BReP142MZwHobM41XNQT9gOL5Px9O5o+dFtftwGsWKGbp3TdBEC3AxOEoLsuHvyJO9GKi/U8K/tmLQJaxQUR/ksoaM4/8PYAIMatrywhic8AuQBFY+ghrgHk/XLKWRp6FfMHL0Bc8X8BDQx7eBIQiES03UCcgWWJ4U/c9TrASfnTj8vJRIXdOZVzFooS6TX5u4LXpYqVGW6sVE6ADeDyHIAABzWCWVhe847b9iZIj7lFl/1aE6SAc7k8Yn+1LJq/b9fLKZ9DaN2Nx5jk2QQEF7HblIoBjDmv5GceTX8OEZwPYQPo/5PsCkCSFhsLa9ekeuXXV7k2BVLGqFWIWMpc+k0pZaMZNqv3v72NF4scp0eVO7iVmbGBEJRPPPtDgK1ap/8KJzcAdoHRhLkTp52t+bQGJGL5BZGE/5EUnt+aIlaQql5dDYfrquahpiYj5peKAeQVTsnTNHsF+LlTdTH+L3+HKD30weaUD11JnAEUSvI1elmNcmEe2E8dhEeMWU5CVHEc4xQBHpG8J/B5R82hQ9sDqQpjNlavJaBSzvYrUpftDvyCUVbjijWeI2c3XhCTR4yI3guEQvOdQkE5ddsTY33lZSRsvyAeOEIqBhBFCVaHYMerpx74P3el8uoXU4RbA7DDHyWVGgtX2vj/A/xp4wunXY9CKBYZx8AMIvBLMJe2ap31sD3Kn5uCQKHGqhuQcCUgOyBIGd2/G2U1D6RQbbys2vjCkltQ0ziMOMoSd4TCgQWpRgXGhAk1VE8USJsIkMfjkIoBKKxM9GVuBMIpTtegCh04HDZn79+9NSX/n1hDw41VFyPAWuLtNdXs/7xmWjco/q9B6HQcXzT1akT9EURn3xwiarekrBgIpGR7Pm9cfFIYMrYj8OhokeCARv5it+GPXL0pvyBd2LeDCf2UopcBWzHQvvDjd7d/xS0/UTpavSLbGtZVT0AqTNYZnI3ouwhvQO1RNvs+FcQ9BKvn7xE53keFzUCOcZGgo0Hfe8IBvOLAuy99PhAN7Xis8nifR6iHj0udyyM/kaw0xmTV4a/cQWM7lx1JkTd5+vlI2CAYzCU2No6Uc1p2NA3IarSnosI4Z1JOlQS8myUvwd261XZHjP+XlcdFooiToPE8AkxMFB9gw2JKuT7o9y9NFSEiJh5tmqmF20+9FkHcB4zVGAjeDITlzKzrV8YNRorvCzRhgmf8iDHL0XHva0Nh1HeY3y79YvfulBzQevZDqKH6SURwxgJSyxzAXL28ZrOLfnSVNBoiqVDxJjllJIAPpEVXt+7c0uqUlvs/EiYJvCDvCHF0KrEQCcVSToKZp2bdT4A3JIoP6D4LaR01h7anfhaKCRWFjlFeuo48Dgj0ou4QC5HQGzS3aNocDYUike43JjO6zLUhwJKPm7eoA2jS4Y99tR5uqFI4RDc6IYMhwFdEMMcor3EFi80dfCrdqWeWjMjMwTWI4kpEjHsAs2c9oC3Bzq75AzXrqfrD9dUXQcQAnM4gHdKS87wVK5N+gGPoReQVlVyhCU0hdeRgPII+ojYL5eLWNzP+oy8YMKOOuEnCDdUXIFKj4zlAgQED1evltYsS1ZcwHiCvcMa5QuDjAPSL/s4B0duftwIkF32SwqNPfwIq4mTU8DWIvMDG+5ST0zY9KObiIh7+Z3LKL9bHF2ZfhwISHoQVMhpI+bu9O32PDGSnU2P1SSaAcmpzAgd+wwqGFvgWrTqYXDt5ucb984Vn6J7MtUA4sb+3kYiCtcemAAAExElEQVQfEP0ViObtbW56j1cqLxWtWTbC8srHCRSdaqLbIGojMi/xlD+U8C0koQHk5k8/TsvEuxCwrL87cBX9BRIe77CCd6Ya+dO3+fTorSNNn7UXMCEiQCcCLdLLBm/Jj8k1pqgk34Oagvo4Jy5SCtGHphVauG/Xqyk//PTUh8LMH+EbrvB5nHjENndRuCwnSTAA3hAEsMHDMjPscYGIEZb7Hp/CQpVIT2Kg85aWd15PiTCjP5nM+qqrSKgHwgQv0kzPAMeg+LyikpkCNcXLdVpPa4/O/p+RtJa27Mx8aSBnPNVoeqIix9SPeQEIfpFgFXhfbw+dzwV85XZwf+mi7uH3ISoOXRjWc0WMbgU7iXBDi/VNFQzgWcjWxYM3ZZjZ+p2IUErx0JoJjqg7I70rcBcbDDh5hYi8ohkKw/8eABj943EBn1tAy/Y1b1HvSAP+2WNDy3kNEM/tt3ClC0Ef6t+HL3caG44G8NNJF5zg9WRWIohSAMgGJA0ATSCp+Kb+SJ2BO1vfdxfyx9FI5MR/ylkIunpbuBSw13InAUhRnt5mZB1awwV85dSbII0YWzh9oiHgfgQ8lwB9CIRqrUdQcRD0jmnh7f+7a8uOgXgL6TWjrgDhP3HJybrmvRfRRkrrewVoIdCWkBm8MeO1//tywLxhEygjt+D8UULPXIYoZgHgcADSgVCdgdolwdPBoHnfoXdfGZDrzx+tMJtmalbHT0sJaKXtG9R3bBBsI826wxj22XtOY8PRANQ+a+zEaZM0Ly4QICYD0rFA8DUR7AKkp1uaO3YeZQhPcYj1XUrjO4QFiGidYcJDyCS/GAjJxk6alC30kVdqqF8DiOcggFfddgDRRwhW4w/ff/9ijAlzIOrrW4ZZVzWNBDzzoytAdRNGME9PBvwqaUHzjdzCEydpQswGQDUujgeCNiL4syS5cd/OJkUEknQopJNY9Niy08OGVY0I1/RxFlQPgbcd6vSv5sDicAwAAIr1vMlZk5HEpYhwPBF8AWHrTy1vNSm2EctJ2FT+K4cwJLypJzweovI8pVqjbOXuVMpOJu/p/1T8D17v8CsFiCJQmEEEfpLwVtDqfPqTt1//OpkyuXmocUFWmEYqAo9ekOGI2N7+bVvFscvcM6Vz646TTuROLCnUvfoUAhqlHkRJWk2tRz7ZmQo8OlemgRgbTAOIUiSNPDYrFLB0C8Jhf9DsSCXul9tIFSAOWtZPgsLqXvYJQfpCXd9wsB+59bDTFRfruR1Zx2hea5jUNSFMS4Y16trf7D8yWCthTDaFmx84ueMU7KEL9Y+kZvm+yPp8oHz/2boAAPVgOtKrZwXBMDw+zfzu74c7vv14e+dAXonHk2cgxgbbANwoJZ02rYGhooG0AQyVnkrLOSgaSBvAoKg1XehQ0UDaAIZKT6XlHBQNpA1gUNSaLnSoaCBtAEOlp9JyDooG0gYwKGpNFzpUNJA2gKHSU2k5B0UDaQMYFLWmCx0qGkgbwFDpqbScg6KBtAEMilrThQ4VDaQNYKj0VFrOQdFA2gAGRa3pQoeKBtIGMFR6Ki3noGggbQCDotZ0oUNFA2kDGCo9lZZzUDSQNoBBUWu60KGigbQBDJWeSss5KBpIG8CgqDVd6FDRwP8DafWkVT2A23oAAAAASUVORK5CYII=";
            var messageId = "1";
            var attachmentId = "2";
            var email = new Message()
            {
                InternalDate = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
                Payload = new MessagePart()
                {
                    Parts = new List<MessagePart>()
                    {
                        new MessagePart()
                        {
                            Body = new MessagePartBody()
                            {
                                AttachmentId = attachmentId
                            },
                            Filename = "jpg"
                        }
                    }
                }
            };
            gmailApiMock.Setup(x => x.GetAttachment(messageId, attachmentId)).ReturnsAsync(base64Image);
            var updater = new MessageUpdater(loggerMock.Object, config, gmailApiMock.Object, memetricsApiMock.Object);
            var attachments = await updater.GetAttachments(messageId, email);

            gmailApiMock.Verify(x => x.GetAttachment(messageId, attachmentId), Times.Once);
            Assert.Single(attachments);
            Assert.Equal(attachmentId, attachments.First().AttachmentId);
            Assert.Equal(base64Image, attachments.First().Base64Data);
            Assert.Equal("jpg", attachments.First().FileName);
        }
    }
}
