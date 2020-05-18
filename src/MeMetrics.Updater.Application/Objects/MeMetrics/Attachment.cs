namespace MeMetrics.Updater.Application.Objects.MeMetrics
{
    public class Attachment
    {
        public Attachment(string attachmentId, string base64Data, string fileName)
        {
            AttachmentId = attachmentId;
            Base64Data = base64Data;
            FileName = fileName;
        }

        public string AttachmentId { get; set; }
        public string Base64Data { get; set; }
        public string FileName { get; set; }
    }
}