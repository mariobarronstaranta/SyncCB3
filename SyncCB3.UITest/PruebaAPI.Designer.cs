namespace SyncCB3.UITest
{
    partial class PruebaAPI
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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
            lblURL = new Label();
            label2 = new Label();
            label3 = new Label();
            txtURL = new TextBox();
            txtUrlGetOrders = new TextBox();
            txtUrlPedidos = new TextBox();
            btnAccion = new Button();
            btnPedidos = new Button();
            btnSendOrders = new Button();
            txtMensajes = new TextBox();
            SuspendLayout();
            // 
            // lblURL
            // 
            lblURL.AutoSize = true;
            lblURL.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold);
            lblURL.Location = new Point(17, 16);
            lblURL.Name = "lblURL";
            lblURL.Size = new Size(198, 24);
            lblURL.TabIndex = 0;
            lblURL.Text = "URL CB3 CLIENTES";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold);
            label2.Location = new Point(17, 63);
            label2.Name = "label2";
            label2.Size = new Size(188, 24);
            label2.TabIndex = 1;
            label2.Text = "URL CB3 PEDIDOS";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold);
            label3.Location = new Point(17, 108);
            label3.Name = "label3";
            label3.Size = new Size(232, 24);
            label3.TabIndex = 2;
            label3.Text = "URL CB3 LIST ORDERS";
            // 
            // txtURL
            // 
            txtURL.Location = new Point(304, 6);
            txtURL.Name = "txtURL";
            txtURL.Size = new Size(853, 27);
            txtURL.TabIndex = 3;
            // 
            // txtUrlGetOrders
            // 
            txtUrlGetOrders.Location = new Point(304, 98);
            txtUrlGetOrders.Name = "txtUrlGetOrders";
            txtUrlGetOrders.Size = new Size(853, 27);
            txtUrlGetOrders.TabIndex = 4;
            // 
            // txtUrlPedidos
            // 
            txtUrlPedidos.Location = new Point(304, 53);
            txtUrlPedidos.Name = "txtUrlPedidos";
            txtUrlPedidos.Size = new Size(853, 27);
            txtUrlPedidos.TabIndex = 5;
            // 
            // btnAccion
            // 
            btnAccion.BackColor = Color.FromArgb(192, 255, 192);
            btnAccion.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold);
            btnAccion.Location = new Point(1179, 11);
            btnAccion.Margin = new Padding(4);
            btnAccion.Name = "btnAccion";
            btnAccion.Size = new Size(137, 34);
            btnAccion.TabIndex = 6;
            btnAccion.Text = "ENVIAR";
            btnAccion.UseVisualStyleBackColor = false;
            btnAccion.Click += btnAccion_Click;
            // 
            // btnPedidos
            // 
            btnPedidos.BackColor = Color.FromArgb(192, 255, 255);
            btnPedidos.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Bold);
            btnPedidos.Location = new Point(1179, 53);
            btnPedidos.Margin = new Padding(4);
            btnPedidos.Name = "btnPedidos";
            btnPedidos.Size = new Size(137, 37);
            btnPedidos.TabIndex = 7;
            btnPedidos.Text = "ENVIAR";
            btnPedidos.UseVisualStyleBackColor = false;
            // 
            // btnSendOrders
            // 
            btnSendOrders.BackColor = Color.FromArgb(255, 255, 192);
            btnSendOrders.Font = new Font("Microsoft Sans Serif", 11.25F, FontStyle.Bold);
            btnSendOrders.Location = new Point(1179, 97);
            btnSendOrders.Margin = new Padding(4);
            btnSendOrders.Name = "btnSendOrders";
            btnSendOrders.Size = new Size(137, 37);
            btnSendOrders.TabIndex = 8;
            btnSendOrders.Text = "SYNC";
            btnSendOrders.UseVisualStyleBackColor = false;
            // 
            // txtMensajes
            // 
            txtMensajes.Font = new Font("Arial", 11F);
            txtMensajes.Location = new Point(21, 196);
            txtMensajes.Margin = new Padding(4);
            txtMensajes.Multiline = true;
            txtMensajes.Name = "txtMensajes";
            txtMensajes.Size = new Size(1293, 445);
            txtMensajes.TabIndex = 9;
            // 
            // PruebaAPI
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.GradientInactiveCaption;
            ClientSize = new Size(1344, 663);
            Controls.Add(txtMensajes);
            Controls.Add(btnSendOrders);
            Controls.Add(btnPedidos);
            Controls.Add(btnAccion);
            Controls.Add(txtUrlPedidos);
            Controls.Add(txtUrlGetOrders);
            Controls.Add(txtURL);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(lblURL);
            Name = "PruebaAPI";
            Text = "FrmClienteCB3";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblURL;
        private Label label2;
        private Label label3;
        private TextBox txtURL;
        private TextBox txtUrlGetOrders;
        private TextBox txtUrlPedidos;
        private Button btnAccion;
        private Button btnPedidos;
        private Button btnSendOrders;
        private TextBox txtMensajes;
    }
}