using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using RT.Util.Dialogs;
//using XPTable.Models;

namespace RT.Util.Controls
{
    public partial class ItemsEditor : Component
    {
        private Control container;

        public ItemsEditor(Control container)
        {
            InitializeComponent();
            this.container = container;
        }

        public ItemsEditor(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }

        private Button FBtnSave = null;
        private Button FBtnCancel = null;
        private Button FBtnDelete = null;
        private Button FBtnNew = null;
        private Button FBtnEdit = null;
        private Form FParentForm = null;

        private string FPropertyPrefix = "Prop";
        private ItemsEditorMode FMode = ItemsEditorMode.Viewing;
        private bool FModified = false;

        private int FUpdating = 0;
        private bool FStuffRegistered = false;

        public void BeginUpdate()
        {
            if (FUpdating == 0)
                UnregisterStuff();
            FUpdating++;
        }

        public void EndUpdate()
        {
            if (FUpdating == 0)
                throw new Exception("EndUpdate called when the control is not in the BeginUpdate mode.");
            FUpdating--;
            if (FUpdating == 0)
                RegisterStuff();
        }

        private bool NecessaryFieldsAssigned
        {
            get
            {
                if (FBtnSave == null) return false;
                if (FBtnCancel == null) return false;
                if (FBtnDelete == null) return false;
                if (FBtnNew == null) return false;
                if (FBtnEdit == null) return false;
                if (FParentForm == null) return false;
                return true;
            }
        }

        private void RegisterStuff()
        {
            if (FUpdating > 0)
                return;
            if (FStuffRegistered)
                return;
            if (!NecessaryFieldsAssigned)
                return;

            // Determine the parent form
            //FParentForm = this.Top
            //if (FParentForm == null)
            //    throw new Exception("Could not find a form containing the BtnCancel button.");
            //Control ctl = FBtnCancel.FindForm();
            //while ((ctl != null) && !(ctl is Form))
            //    ctl = ctl.Parent;
            //if (ctl == null)
            //    throw new Exception("Could not find a form containing the BtnCancel button.");
            //FParentForm = (Form)ctl;

            FBtnCancel.Click += new EventHandler(FBtnCancel_Click);
            FBtnDelete.Click += new EventHandler(FBtnDelete_Click);
            FBtnEdit.Click += new EventHandler(FBtnEdit_Click);
            FBtnNew.Click += new EventHandler(FBtnNew_Click);
            FBtnSave.Click += new EventHandler(FBtnSave_Click);
            FParentForm.FormClosing += new FormClosingEventHandler(FParentForm_FormClosing);

            FStuffRegistered = true;
        }

        private void UnregisterStuff()
        {
            if (FUpdating > 0)
                return;
            if (!FStuffRegistered)
                return;
                //throw new Exception("UnregisterStuff called when stuff already unregistered.");

            if (FBtnEdit != null) FBtnEdit.Click -= new EventHandler(FBtnEdit_Click);
            if (FBtnDelete != null) FBtnDelete.Click -= new EventHandler(FBtnDelete_Click);
            if (FBtnNew != null) FBtnNew.Click -= new EventHandler(FBtnNew_Click);
            if (FBtnSave != null) FBtnSave.Click -= new EventHandler(FBtnSave_Click);
            if (FBtnCancel != null) FBtnCancel.Click -= new EventHandler(FBtnCancel_Click);
            if (FParentForm != null) FParentForm.FormClosing -= new FormClosingEventHandler(FParentForm_FormClosing);

            FStuffRegistered = false;
        }

        private bool UpdatingControls = false;

        public void UpdateControls()
        {
            UpdateControls(false);
        }

        private void UpdateControls(bool Entering)
        {
            if (FUpdating > 0)
                return;

            // Avoid reentry
            if (UpdatingControls)
                return;
            UpdatingControls = true;

            //// Get selection
            //Row r = null;
            //Person p = null;
            //bool MultiSelected = LV.SelectedItems.Length > 1;
            //bool SingleSelected = LV.SelectedItems.Length == 1;
            //bool NoneSelected = LV.SelectedItems.Length == 0;
            //if (!NoneSelected)
            //{
            //    r = LV.SelectedItems[0];
            //    p = (Person)r.Tag;
            //}

            //// Enables and visibility etcs
            //LV.Enabled = FMode == ItemsEditorMode.Viewing;
            //BtnNew.Visible = BtnEdit.Visible = BtnDelete.Visible = FMode == ItemsEditorMode.Viewing;
            //BtnSave.Visible = BtnCancel.Visible = FMode != ItemsEditorMode.Viewing;
            //BtnEdit.Enabled = SingleSelected;
            //BtnDelete.Enabled = (r != null);
            //EAlias.ReadOnly = FMode == ItemsEditorMode.Viewing;
            //EName.ReadOnly = FMode == ItemsEditorMode.Viewing;

            //// Update labels; select default button if Editing/Creating
            //if (FMode == ItemsEditorMode.Viewing)
            //{
            //    sep.Text = "Viewing person";
            //    AcceptButton = null;
            //}
            //else if (FMode == ItemsEditorMode.Editing)
            //{
            //    sep.Text = "Editing person";
            //    if (Modified && !Entering)
            //        sep.Text += " *";
            //    BtnSave.Text = "&Save";
            //    AcceptButton = BtnSave;
            //}
            //else if (FMode == ItemsEditorMode.Creating)
            //{
            //    sep.Text = "Creating new person";
            //    if (Modified && !Entering)
            //        sep.Text += " *";
            //    BtnSave.Text = "&Create";
            //    AcceptButton = BtnSave;
            //}

            //// Update the ItemForm
            //if ((SingleSelected && (FMode == ItemsEditorMode.Viewing)) || (Entering && (FMode == ItemsEditorMode.Editing)))
            //{
            //    EAlias.Text = p.Alias;
            //    EName.Text = p.Name;
            //}
            //else if (MultiSelected && (FMode == ItemsEditorMode.Viewing))
            //{
            //    EAlias.Text = "(multiple)";
            //    EName.Text = "(multiple)";
            //}
            //else if (NoneSelected && (FMode == ItemsEditorMode.Viewing))
            //{
            //    EAlias.Text = "(none)";
            //    EName.Text = "(none)";
            //}
            //else if (Entering && (FMode == ItemsEditorMode.Creating))
            //{
            //    EAlias.Text = "";
            //    EName.Text = "";
            //}

            //// Focus the first field if entering
            //if (Entering)
            //    EAlias.Focus();

            // Done
            UpdatingControls = false;
        }

        public string PropertyPrefix
        {
            get { return FPropertyPrefix; }
            set { UnregisterStuff(); FPropertyPrefix = value; RegisterStuff(); }
        }

        public Button BtnSave
        {
            get { return FBtnSave; }
            set { UnregisterStuff(); FBtnSave = value; RegisterStuff(); }
        }

        public Button BtnCancel
        {
            get { return FBtnCancel; }
            set { UnregisterStuff(); FBtnCancel = value; RegisterStuff(); }
        }

        public Button BtnDelete
        {
            get { return FBtnDelete; }
            set { UnregisterStuff(); FBtnDelete = value; RegisterStuff(); }
        }

        public Button BtnNew
        {
            get { return FBtnNew; }
            set { UnregisterStuff(); FBtnNew = value; RegisterStuff(); }
        }

        public Button BtnEdit
        {
            get { return FBtnEdit; }
            set { UnregisterStuff(); FBtnEdit = value; RegisterStuff(); }
        }

        public Form ParentForm
        {
            get { return FParentForm; }
            set { UnregisterStuff(); FParentForm = value; RegisterStuff(); }
        }

        void FBtnCancel_Click(object sender, EventArgs e)
        {
            // Confirm
            if (FModified)
                if (DlgMessage.ShowWarning("Are you sure you want to cancel editing and lose the changes you have made?", "&Forget the changes", "Continue &editing") == 1)
                    return;

            // Nothing's modified anymore
            FModified = false;

            FMode = ItemsEditorMode.Viewing;
            UpdateControls();
            //LV.Focus();
        }

        void FBtnDelete_Click(object sender, EventArgs e)
        {
            if (DlgMessage.Show("Are you sure you want to delete the selected people?", "Delete people", DlgType.Question, "&Delete", "&Cancel") == 1)
                return;

            //// Store focused cell
            //CellPos fc = LV.FocusedCell;

            //// Delete
            //foreach (Row r in LV.SelectedItems)
            //{
            //    DB.Person.Remove((Person)r.Tag);
            //    LVrows.Rows.Remove(r);
            //}

            //// Select & focus closest
            //LVrows.Selections.Clear();
            //if (fc.Row >= LVrows.Rows.Count)
            //    fc.Row = LVrows.Rows.Count - 1;
            //if (fc.Row >= 0)
            //{
            //    LVrows.Selections.AddCell(fc.Row, 0);
            //    LV.FocusedCell = fc;
            //}
        }

        void FBtnEdit_Click(object sender, EventArgs e)
        {
            FMode = ItemsEditorMode.Editing;
            UpdateControls(true);
            FModified = false;
        }

        void FBtnNew_Click(object sender, EventArgs e)
        {
            FMode = ItemsEditorMode.Creating;
            UpdateControls(true);
            FModified = false;
        }

        void FBtnSave_Click(object sender, EventArgs e)
        {
            //// Get the object
            //Person p;
            //if (FMode == ItemsEditorMode.Creating)
            //{
            //    p = new Person();
            //    DB.Person.Add(p);
            //}
            //else
            //    p = (Person)LV.SelectedItems[0].Tag;

            //// Save new properties
            //p.Alias = EAlias.Text;
            //p.Name = EName.Text;

            //FMode = ItemsEditorMode.Viewing;
            //RebuildLV();
            //UpdateControls();

            //// Select the saved entry
            //LV.Focus();
            //LVrows.Selections.Clear();
            //foreach (Row r in LVrows.Rows)
            //    if ((Person)r.Tag == p)
            //    {
            //        LVrows.Selections.AddCell(r.Index, 0);
            //        LV.FocusedCell = new CellPos(r.Index, 0);
            //        break;
            //    }

            //// Set Modified to False as a means of communicating success to the
            //// FormClose event
            //FModified = false;
        }

        void FParentForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if ((FMode == ItemsEditorMode.Viewing) || !FModified)
                return;

            int ans = DlgMessage.ShowWarning("You are about to close the form while still editing an item. What would you like to do?", "&Save changes and close", "&Discard changes and close", "&Cancel");

            //if (ans == 0)
            //{
            //    // Save changes and close
            //    BtnSave_Click(null, null);
            //    // If this failed then cancel
            //    if (FModified)
            //        e.Cancel = true;
            //}
            //else if (ans == 1)
            //{
            //    // Discard changes and close

            //    // do nothing else
            //}
            //else
            //{
            //    // Cancel
            //    e.Cancel = true;
            //}
        }

        /// <summary>
        /// Form mode - determines the state the form is in.
        /// </summary>
        public ItemsEditorMode Mode
        {
            get { return FMode; }
        }

        /// <summary>
        /// Set if controls on the form have been changed after entering Create
        /// or Edit mode.
        /// </summary>
        public bool Modified
        {
            get { return FModified; }
        }

    }

    public enum ItemsEditorMode
    {
        Viewing,
        Editing,
        Creating,
    }
}
