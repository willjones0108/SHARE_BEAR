﻿namespace JMUcare.Pages.Dataclasses
{
    public class ProjectTaskModel
    {
        public int TaskID { get; set; }
        public int ProjectID { get; set; }
        public string TaskContent { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; }
        public int PhasePosition { get; set; }

        public List<DbUserModel> AssignedUsers { get; set; } = new List<DbUserModel>();
    }
}
