using System.ComponentModel;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;

namespace SmsPing;

public partial class frmMain
{
	private IContainer components = null;

	private GroupBox grpConnect;
	private ComboBox cmbPort;
	private Button btnConnect;
	private Button btnDisconnect;
	private Label lblStatus;

	private GroupBox grpMode;
	private RadioButton rbPing;
	private RadioButton rbAt;

	private Panel panelPing;
	private Label lblTarget;
	private TextBox txtTarget;
	private CheckBox ckbBao;
	private TextBox txtNotify;
	private Button btnSend;
	private Button btnXoaBao;

	private Panel panelAt;
	private Label lblAt;
	private TextBox txtAt;
	private CheckBox ckbCr;
	private Button btnSendAt;
	private Button btnCtrlZ;

	private GroupBox grpRaw;
	private TextBox txtRaw;
	private GroupBox grpDecode;
	private TextBox txtDecode;

	private GroupBox grpTool;
	private Button btn_chkHard;
	private Button btn_TK;
	private Button btnDecodeSel;
	private Button btnClr;
	private Button btnHelp;
	private Button btnAbout;

	private SerialPort SerialPort1;
	private Timer TimerConnect;
	private Timer TimerPingOk;
	private Timer TimerChoKq;

	protected override void Dispose(bool disposing)
	{
		if (disposing) components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		components = new Container();
		SerialPort1 = new SerialPort(components);
		SerialPort1.DataReceived += SerialPort1_DataReceived;

		TimerConnect = new Timer(components) { Interval = 500 };
		TimerConnect.Tick += TimerConnect_Tick;
		TimerPingOk = new Timer(components) { Interval = 1500 };
		TimerPingOk.Tick += TimerPingOk_Tick;
		TimerChoKq = new Timer(components) { Interval = 5000 };
		TimerChoKq.Tick += TimerChoKq_Tick;

		Font baseFont = new Font("Segoe UI", 9F);
		Color accent = Color.FromArgb(21, 101, 192);
		Color panelBg = Color.White;

		// ---- Kết nối ----
		grpConnect = new GroupBox
		{
			Text = "Kết nối modem",
			Location = new Point(16, 12),
			Size = new Size(660, 90),
			ForeColor = accent,
			Font = baseFont
		};
		cmbPort = new ComboBox { Location = new Point(16, 28), Size = new Size(180, 24), DropDownStyle = ComboBoxStyle.DropDownList };
		cmbPort.SelectedIndexChanged += cmbPort_SelectedIndexChanged;
		btnConnect = new Button { Text = "Connect", Location = new Point(206, 27), Size = new Size(90, 26), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(46, 125, 50), ForeColor = Color.White };
		btnConnect.Click += btnConnect_Click;
		btnDisconnect = new Button { Text = "Disconnect", Location = new Point(302, 27), Size = new Size(90, 26), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(198, 40, 40), ForeColor = Color.White };
		btnDisconnect.Click += btnDisconnect_Click;
		lblStatus = new Label { Text = "Chưa kết nối", Location = new Point(16, 60), AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Red };
		grpConnect.Controls.AddRange(new Control[] { cmbPort, btnConnect, btnDisconnect, lblStatus });

		// ---- Chế độ ----
		grpMode = new GroupBox { Text = "Chọn chế độ lệnh SEND", Location = new Point(16, 110), Size = new Size(660, 54), ForeColor = accent, Font = baseFont };
		rbPing = new RadioButton { Text = "PING SMS", Location = new Point(16, 22), AutoSize = true, Checked = true };
		rbPing.CheckedChanged += rbPing_CheckedChanged;
		rbAt = new RadioButton { Text = "AT Command", Location = new Point(140, 22), AutoSize = true };
		rbAt.CheckedChanged += rbAt_CheckedChanged;
		grpMode.Controls.AddRange(new Control[] { rbPing, rbAt });

		// ---- Panel PING SMS ----
		panelPing = new Panel { Location = new Point(16, 172), Size = new Size(660, 130), BackColor = panelBg, BorderStyle = BorderStyle.FixedSingle };
		lblTarget = new Label { Text = "SĐT cần PING:", Location = new Point(12, 14), AutoSize = true };
		txtTarget = new TextBox { Location = new Point(12, 34), Size = new Size(160, 24), MaxLength = 10, Text = "0" };
		ckbBao = new CheckBox { Text = "Có yêu cầu báo khi SĐT online lại", Location = new Point(12, 68), AutoSize = true };
		ckbBao.CheckedChanged += ckbBao_CheckedChanged;
		txtNotify = new TextBox { Location = new Point(12, 92), Size = new Size(160, 24), MaxLength = 10, Text = "0", Enabled = false };
		btnSend = new Button { Text = "GỬI PING", Location = new Point(200, 34), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.White };
		btnSend.Click += btnSend_Click;
		btnXoaBao = new Button { Text = "Xoá mọi yêu cầu báo", Location = new Point(200, 82), Size = new Size(150, 28), FlatStyle = FlatStyle.Flat };
		btnXoaBao.Click += btnXoaBao_Click;
		panelPing.Controls.AddRange(new Control[] { lblTarget, txtTarget, ckbBao, txtNotify, btnSend, btnXoaBao });

		// ---- Panel AT Command ----
		panelAt = new Panel { Location = new Point(16, 172), Size = new Size(660, 130), BackColor = panelBg, BorderStyle = BorderStyle.FixedSingle, Visible = false };
		lblAt = new Label { Text = "Lệnh AT:", Location = new Point(12, 14), AutoSize = true };
		txtAt = new TextBox { Location = new Point(12, 34), Size = new Size(220, 24) };
		ckbCr = new CheckBox { Text = "+CR", Location = new Point(12, 68), AutoSize = true, Checked = true };
		btnSendAt = new Button { Text = "SEND AT", Location = new Point(240, 34), Size = new Size(100, 26), FlatStyle = FlatStyle.Flat, BackColor = accent, ForeColor = Color.White };
		btnSendAt.Click += btnSendAt_Click;
		btnCtrlZ = new Button { Text = "Gửi Ctrl+Z", Location = new Point(12, 96), Size = new Size(140, 26), FlatStyle = FlatStyle.Flat, BackColor = Color.FromArgb(96, 96, 96), ForeColor = Color.White };
		btnCtrlZ.Click += btnCtrlZ_Click;
		panelAt.Controls.AddRange(new Control[] { lblAt, txtAt, ckbCr, btnSendAt, btnCtrlZ });

		// ---- RAW CODE ----
		grpRaw = new GroupBox { Text = "RAW CODE", Location = new Point(16, 312), Size = new Size(324, 220), ForeColor = accent, Font = baseFont };
		txtRaw = new TextBox { Location = new Point(10, 24), Size = new Size(300, 184), Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Consolas", 8.5F) };
		grpRaw.Controls.Add(txtRaw);

		// ---- DECODE ----
		grpDecode = new GroupBox { Text = "DECODE (bôi đen đoạn REPORT trong RAW CODE rồi bấm DECODE)", Location = new Point(352, 312), Size = new Size(324, 220), ForeColor = accent, Font = baseFont };
		txtDecode = new TextBox { Location = new Point(10, 24), Size = new Size(300, 184), Multiline = true, ScrollBars = ScrollBars.Vertical, ReadOnly = true, Font = new Font("Consolas", 8.5F) };
		grpDecode.Controls.Add(txtDecode);

		// ---- TOOL ----
		grpTool = new GroupBox { Text = "Tool", Location = new Point(16, 542), Size = new Size(660, 96), ForeColor = accent, Font = baseFont };
		btn_chkHard = new Button { Text = "Check Hardware", Location = new Point(12, 24), Size = new Size(120, 28), FlatStyle = FlatStyle.Flat };
		btn_chkHard.Click += btn_chkHard_Click;
		btn_TK = new Button { Text = "Check TK SIM", Location = new Point(140, 24), Size = new Size(120, 28), FlatStyle = FlatStyle.Flat };
		btn_TK.Click += btn_TK_Click;
		btnDecodeSel = new Button { Text = "DECODE", Location = new Point(268, 24), Size = new Size(100, 28), FlatStyle = FlatStyle.Flat };
		btnDecodeSel.Click += btnDecodeSel_Click;
		btnClr = new Button { Text = "CLEAR", Location = new Point(376, 24), Size = new Size(100, 28), FlatStyle = FlatStyle.Flat };
		btnClr.Click += btnClr_Click;
		btnHelp = new Button { Text = "Help", Location = new Point(484, 24), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat };
		btnHelp.Click += btnHelp_Click;
		btnAbout = new Button { Text = "About", Location = new Point(570, 24), Size = new Size(80, 28), FlatStyle = FlatStyle.Flat };
		btnAbout.Click += btnAbout_Click;
		grpTool.Controls.AddRange(new Control[] { btn_chkHard, btn_TK, btnDecodeSel, btnClr, btnHelp, btnAbout });

		// ---- Form ----
		AutoScaleDimensions = new SizeF(96F, 96F);
		AutoScaleMode = AutoScaleMode.Dpi;
		ClientSize = new Size(692, 650);
		Font = baseFont;
		FormBorderStyle = FormBorderStyle.FixedSingle;
		MaximizeBox = false;
		StartPosition = FormStartPosition.CenterScreen;
		Text = "SmsPing - Ver LTE";
		BackColor = Color.FromArgb(244, 246, 248);
		try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { /* bỏ qua nếu không lấy được */ }

		Controls.Add(grpConnect);
		Controls.Add(grpMode);
		Controls.Add(panelPing);
		Controls.Add(panelAt);
		Controls.Add(grpRaw);
		Controls.Add(grpDecode);
		Controls.Add(grpTool);
	}
}
