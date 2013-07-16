namespace GW2TP
{
    partial class Listings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.HideButton = new System.Windows.Forms.Button();
            this.DescriptionLabel = new System.Windows.Forms.Label();
            this.ListingsListView = new ListViewEmbeddedControls.ListViewEx();
            this.UnitPrice = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.Quantity = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.NumberOfListings = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.SuspendLayout();
            // 
            // HideButton
            // 
            this.HideButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.HideButton.Location = new System.Drawing.Point(235, 564);
            this.HideButton.Name = "HideButton";
            this.HideButton.Size = new System.Drawing.Size(75, 23);
            this.HideButton.TabIndex = 1;
            this.HideButton.Text = "Hide";
            this.HideButton.UseVisualStyleBackColor = true;
            this.HideButton.Click += new System.EventHandler(this.HideButton_Click);
            // 
            // DescriptionLabel
            // 
            this.DescriptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.DescriptionLabel.AutoSize = true;
            this.DescriptionLabel.Location = new System.Drawing.Point(12, 534);
            this.DescriptionLabel.Name = "DescriptionLabel";
            this.DescriptionLabel.Size = new System.Drawing.Size(0, 13);
            this.DescriptionLabel.TabIndex = 2;
            // 
            // ListingsListView
            // 
            this.ListingsListView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ListingsListView.BackColor = System.Drawing.SystemColors.WindowFrame;
            this.ListingsListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.UnitPrice,
            this.Quantity,
            this.NumberOfListings});
            this.ListingsListView.Font = new System.Drawing.Font("Times New Roman", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ListingsListView.FullRowSelect = true;
            this.ListingsListView.Location = new System.Drawing.Point(2, 12);
            this.ListingsListView.MultiSelect = false;
            this.ListingsListView.Name = "ListingsListView";
            this.ListingsListView.Size = new System.Drawing.Size(555, 519);
            this.ListingsListView.TabIndex = 0;
            this.ListingsListView.UseCompatibleStateImageBehavior = false;
            this.ListingsListView.View = System.Windows.Forms.View.Details;
            // 
            // UnitPrice
            // 
            this.UnitPrice.Text = "Price per unit";
            this.UnitPrice.Width = 132;
            // 
            // Quantity
            // 
            this.Quantity.Text = "Ordered";
            this.Quantity.Width = 98;
            // 
            // NumberOfListings
            // 
            this.NumberOfListings.Text = "Buyers";
            this.NumberOfListings.Width = 130;
            // 
            // Listings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(558, 599);
            this.Controls.Add(this.DescriptionLabel);
            this.Controls.Add(this.HideButton);
            this.Controls.Add(this.ListingsListView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Listings";
            this.Padding = new System.Windows.Forms.Padding(9);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Buy Listings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Listings_FormClosing);
            this.Shown += new System.EventHandler(this.Listings_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ListViewEmbeddedControls.ListViewEx ListingsListView;
        private System.Windows.Forms.Button HideButton;
        private System.Windows.Forms.ColumnHeader UnitPrice;
        private System.Windows.Forms.ColumnHeader Quantity;
        private System.Windows.Forms.ColumnHeader NumberOfListings;
        private System.Windows.Forms.Label DescriptionLabel;

    }
}
