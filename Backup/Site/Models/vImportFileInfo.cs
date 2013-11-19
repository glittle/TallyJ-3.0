using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class vImportFileInfo : Entity
    {
        public System.Guid ElectionGuid { get; set; }
        public Nullable<System.DateTime> UploadTime { get; set; }
        public Nullable<System.DateTime> ImportTime { get; set; }
        public Nullable<int> FileSize { get; set; }
        public Nullable<bool> HasContent { get; set; }
        public Nullable<int> FirstDataRow { get; set; }
        public string ColumnsToRead { get; set; }
        public string OriginalFileName { get; set; }
        public string ProcessingStatus { get; set; }
        public string FileType { get; set; }
        public string Messages { get; set; }
        public Nullable<int> CodePage { get; set; }
    }
}
