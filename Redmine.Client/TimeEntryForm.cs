using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Redmine.Net.Api.Types;
using Redmine.Client.Languages;

namespace Redmine.Client
{
    public partial class TimeEntryForm : BgWorker
    {
        public enum eFormType
        {
            New,
            Edit,
        };
        private TimeEntry CurTimeEntry { get; }
        private readonly RedmineClient redmineClient;
        private readonly Issue issue;
        private IList<ProjectMember> projectMembers;
        private eFormType type;

        public TimeEntryForm(RedmineClient redmineClient, Issue issue, IList<ProjectMember> projectMembers)
        {
            InitializeComponent();
            this.redmineClient = redmineClient;
            this.issue = issue;
            this.projectMembers = projectMembers;
            type = eFormType.New;
            CurTimeEntry = new TimeEntry();
            LoadLanguage();
            LoadCombos();
            comboBoxByUser.SelectedValue = RedmineClientForm.Instance.CurrentUser.Id;
        }
        public TimeEntryForm(RedmineClient redmineClient, Issue issue, IList<ProjectMember> projectMembers, TimeEntry timeEntry)
        {
            InitializeComponent();
            this.redmineClient = redmineClient;
            this.issue = issue;
            this.projectMembers = projectMembers;
            type = eFormType.Edit;
            CurTimeEntry = timeEntry;
            LoadLanguage();
            LoadCombos();

            if (CurTimeEntry.SpentOn.HasValue)
                datePickerSpentOn.Value = CurTimeEntry.SpentOn.Value;

            comboBoxByUser.SelectedValue = CurTimeEntry.User.Id;
            comboBoxActivity.SelectedValue = CurTimeEntry.Activity.Id;
            textBoxSpentHours.Text = CurTimeEntry.Hours.ToString(Lang.Culture);
            textBoxComment.Text = CurTimeEntry.Comments;
        }

        private void LoadCombos()
        {
            comboBoxActivity.DataSource = Enumerations.Activities;
            comboBoxActivity.DisplayMember = "Name";
            comboBoxActivity.ValueMember = "Id";
            foreach (Enumerations.EnumerationItem i in Enumerations.Activities)
            {
                if (i.IsDefault)
                {
                    comboBoxActivity.SelectedValue = i.Id;
                    break;
                }
            }
            comboBoxByUser.DataSource = projectMembers;
            comboBoxByUser.DisplayMember = "Name";
            comboBoxByUser.ValueMember = "Id";
        }

        private void LoadLanguage()
        {
            LangTools.UpdateControlsForLanguage(this.Controls);
            if (type == eFormType.New)
            {
                this.Text = String.Format(Lang.DlgTimeEntryFormTitle_New, issue.Id, issue.Subject);
                //there is a mistake in the language-string, so we added the newline also as the third ({2}) element in the format string
                labelTimeEntryTitle.Text = String.Format(Lang.labelTimeEntryTitle_New, issue.Id, issue.Subject, Environment.NewLine, Environment.NewLine);
            }
            else
            {
                string fmtSpentOn = "";
                if (CurTimeEntry.SpentOn.HasValue)
                    fmtSpentOn = CurTimeEntry.SpentOn.Value.ToString("d", Lang.Culture);
                this.Text = String.Format(Lang.DlgTimeEntryFormTitle_Edit, fmtSpentOn, issue.Id, issue.Subject);
                labelTimeEntryTitle.Text = String.Format(Lang.labelTimeEntryTitle_Edit, fmtSpentOn, issue.Id, issue.Subject, Environment.NewLine);
            }
        }

        private void BtnOKButton_Click(object sender, EventArgs e)
        {
            if (type == eFormType.New)
            {
                CreateTimeEntry();
            }
            else
            {
                UpdateTimeEntry();
            }
            
        }

        private void UpdateTimeEntry()
        {
            
            try
            {
                CurTimeEntry.SpentOn = datePickerSpentOn.Value;
                CurTimeEntry.Issue = IdentifiableName.Create<IdentifiableName>(issue.Id);
                CurTimeEntry.Project = issue.Project;
                //CurTimeEntry.User = new IdentifiableName() { Id = ((ProjectMember)comboBoxByUser.SelectedItem).Id }; not user in api
                CurTimeEntry.Activity = ((Enumerations.EnumerationItem)comboBoxActivity.SelectedItem).ToIdentifiableName();
                CurTimeEntry.Hours = decimal.Parse(textBoxSpentHours.Text, Lang.Culture);
                CurTimeEntry.Comments = textBoxComment.Text;
                redmineClient.UpdateTimeEntry(CurTimeEntry.Id, CurTimeEntry);
                this.DialogResult = DialogResult.OK;
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format(Lang.Error_Exception, ex.Message), Lang.Error, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            
        }

        private void CreateTimeEntry()
        {
            
            try
            {
                var entry = new TimeEntry()
                {
                    Issue = IdentifiableName.Create<IdentifiableName>(issue.Id),
                    Project = IdentifiableName.Create<IdentifiableName>(issue.Project.Id),
                    SpentOn = datePickerSpentOn.Value,
                    Activity = ((Enumerations.EnumerationItem)comboBoxActivity.SelectedItem).ToIdentifiableName(),
                    Hours = decimal.Parse(textBoxSpentHours.Text, Lang.Culture),
                    Comments = textBoxComment.Text
                };
                //CurTimeEntry.User = new IdentifiableName() { Id = ((ProjectMember)comboBoxByUser.SelectedItem).Id }; not user in api
                redmineClient.CreateTimeEntry(entry);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format(Lang.Error_Exception, ex.Message), Lang.Error, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }
    }
}
