using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;

namespace VTV_SMS_Ping;

public partial class frmMain : Form
{
	// Danh sách IMEI thiết bị được phép dùng app (giữ nguyên như bản gốc)
	private static readonly string[] AllowedImei =
	{
		"862636051970828", "862636051979746", "862636054171572", "862636054064009",
		"862636054182835", "862636054166416", "866506050985885", "862636056523887",
		"862636057265306"
	};

	private string _myImei = "";
	private bool _flagCheckImei;
	private string _strDocKq = "";
	private readonly List<DAUVAO> _canBao = new List<DAUVAO>();
	private int _pingOk;

	public frmMain()
	{
		InitializeComponent();
		Load += frmMain_Load;
	}

	private void frmMain_Load(object sender, EventArgs e)
	{
		try
		{
			var ports = SerialPort.GetPortNames();
			cmbPort.Items.AddRange(ports);
			if (ports.Length > 0) cmbPort.SelectedIndex = ports.Length - 1;
			rbPing.Checked = true;
			ckbCr.Checked = true;
			btnDisconnect.Enabled = false;
			SetConnected(false);
		}
		catch (Exception ex)
		{
			MessageBox.Show("Không liệt kê được cổng COM: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	// ---------------- Kết nối modem ----------------

	private void btnConnect_Click(object sender, EventArgs e)
	{
		if (cmbPort.SelectedItem == null)
		{
			MessageBox.Show("Chọn cổng COM có gắn thiết bị PING trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		try
		{
			lblStatus.Text = "Đang kết nối......";
			lblStatus.ForeColor = Color.DarkOrange;
			TimerConnect.Enabled = false;

			SerialPort1.PortName = cmbPort.Text;
			SerialPort1.BaudRate = 9600;
			SerialPort1.Parity = Parity.None;
			SerialPort1.StopBits = StopBits.One;
			SerialPort1.DataBits = 8;
			SerialPort1.Open();

			btnConnect.Enabled = false;
			btnDisconnect.Enabled = true;

			_myImei = "";
			_flagCheckImei = true;
			SerialPort1.Write("AT\r\n");

			int attempts = 0;
			while (!_myImei.Contains("OK") && attempts < 10)
			{
				attempts++;
				SerialPort1.Write("AT\r\n");
				System.Threading.Thread.Sleep(1500);
				Application.DoEvents();
			}
			SerialPort1.Write("AT+CGSN\r\n");

			// Chờ và thử lại tối đa ~10 giây cho tới khi nhận đủ IMEI (15 chữ số),
			// tránh trường hợp modem trả lời chậm qua USB khiến kiểm tra quá sớm bị báo lỗi nhầm.
			int imeiWait = 0;
			while (!System.Text.RegularExpressions.Regex.IsMatch(_myImei, @"\d{15}") && imeiWait < 20)
			{
				imeiWait++;
				System.Threading.Thread.Sleep(500);
				Application.DoEvents();
			}
			CheckImei();
		}
		catch (Exception ex)
		{
			MessageBox.Show("Kiểm tra lại cổng COM (và thiết bị). Có thể ứng dụng khác đã sử dụng cổng COM này.\n" + ex.Message);
		}
	}

	// SMSC mặc định theo từng nhà mạng Việt Nam (dùng khi SIM chưa có sẵn SMSC được lưu)
	private static readonly Dictionary<string, string> SmscByNetwork = new Dictionary<string, string>
	{
		["01"] = "+84900000023", // Mobifone
		["02"] = "+8491020005",  // Vinaphone
		["04"] = "+84980200030", // Viettel
	};

	private static string ExtractCurrentSmsc(string data)
	{
		var m = System.Text.RegularExpressions.Regex.Match(data, "\\+CSCA:\\s*\"([^\"]*)\"");
		return m.Success ? m.Groups[1].Value : "";
	}

	private static string DetectSmscByNetwork(string data)
	{
		var m = System.Text.RegularExpressions.Regex.Match(data, "452-0(\\d)");
		if (m.Success && SmscByNetwork.TryGetValue(m.Groups[1].Value.PadLeft(2, '0'), out string smsc))
			return smsc;
		return null;
	}

	private void CheckImei()
	{
		var match = System.Text.RegularExpressions.Regex.Match(_myImei, @"\d{15}");
		if (match.Success)
		{
			string imei = match.Value;
			if (!AllowedImei.Contains(imei))
			{
				MessageBox.Show("Thiết bị của bạn chưa được đăng ký sử dụng với tác giả.\r\nVui lòng liên hệ tác giả!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
				SerialPort1.Close();
				SetConnected(false);
				_flagCheckImei = false;
				return;
			}
		}
		else
		{
			MessageBox.Show("Cổng COM chưa nhận được thiết bị!\r\nThử kết nối lại.\r\n\r\n--- Dữ liệu đã nhận được ---\r\n" + _myImei, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
			SerialPort1.Close();
			SetConnected(false);
			_flagCheckImei = false;
			return;
		}

		SerialPort1.Write("AT+CNMP=38\r\n");
		System.Threading.Thread.Sleep(300);
		Application.DoEvents();

		// Kiểm tra SIM đã có sẵn SMSC chưa (mỗi SIM nhà mạng thường tự lưu sẵn số này)
		SerialPort1.Write("AT+CSCA?\r\n");
		System.Threading.Thread.Sleep(300);
		Application.DoEvents();
		string currentSmsc = ExtractCurrentSmsc(_myImei);

		SerialPort1.Write("AT+CPSI?\r\n");
		System.Threading.Thread.Sleep(500);
		Application.DoEvents();

		if (string.IsNullOrEmpty(currentSmsc))
		{
			// SIM chưa có SMSC -> tự nhận diện nhà mạng qua mã 452-xx và set SMSC tương ứng
			string autoSmsc = DetectSmscByNetwork(_myImei);
			if (autoSmsc != null)
			{
				SerialPort1.Write($"AT+CSCA=\"{autoSmsc}\"\r\n");
				System.Threading.Thread.Sleep(300);
			}
			else
			{
				MessageBox.Show("Không tự nhận diện được nhà mạng để set SMSC.\r\nAnh vào chế độ AT Command, gửi lệnh: AT+CSCA=\"số SMSC nhà mạng\" trước khi PING.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}
		// Nếu SIM đã có SMSC sẵn (currentSmsc khác rỗng) thì giữ nguyên, không ghi đè

		SerialPort1.Write("AT+CNMI=1,0,0,1,0\r\n");
		System.Threading.Thread.Sleep(200);
		SerialPort1.Write("AT+CLIP=1\r\n");
		txtTarget.Focus();
		_flagCheckImei = false;
		SetConnected(true);
	}

	private void btnDisconnect_Click(object sender, EventArgs e)
	{
		try
		{
			SerialPort1.Close();
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message);
		}
		finally
		{
			SetConnected(false);
		}
	}

	private void SetConnected(bool connected)
	{
		btnConnect.Enabled = !connected;
		btnDisconnect.Enabled = connected;
		TimerConnect.Enabled = connected;
		lblStatus.Text = connected ? "Đã kết nối" : "Chưa kết nối";
		lblStatus.ForeColor = connected ? Color.Green : Color.Red;
	}

	private void TimerConnect_Tick(object sender, EventArgs e)
	{
		bool open = SerialPort1.IsOpen;
		lblStatus.Text = open ? "Đã kết nối" : "Chưa kết nối";
		lblStatus.ForeColor = open ? Color.Green : Color.Red;
	}

	private void cmbPort_SelectedIndexChanged(object sender, EventArgs e)
	{
		if (SerialPort1.IsOpen)
		{
			MessageBox.Show("Disconnect trước khi chọn cổng COM", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		SerialPort1.PortName = cmbPort.Text;
	}

	// ---------------- Nhận dữ liệu ----------------

	private void SerialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
	{
		try
		{
			string text = SerialPort1.ReadExisting();
			if (txtRaw.InvokeRequired)
			{
				txtRaw.Invoke(new Action<string>(ReceivedText), text);
			}
			else
			{
				ReceivedText(text);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(ex.Message);
		}
	}

	private void ReceivedText(string text)
	{
		txtRaw.AppendText(text);
		if (_canBao.Count > 0) _strDocKq += text;
		if (_flagCheckImei) _myImei += text;
	}

	// ---------------- Gửi PDU ----------------

	private void SendPdu(string pdu)
	{
		SerialPort1.Write("AT+CMGF=0\r\n");
		System.Threading.Thread.Sleep(200);
		SerialPort1.Write("AT+CMGS=19\r\n");
		System.Threading.Thread.Sleep(300);
		SerialPort1.Write(pdu);
		System.Threading.Thread.Sleep(200);
		SerialPort1.Write("\u001a");
	}

	private static bool ValidPhone(string sdt) =>
		sdt.Length >= 10 && sdt.All(char.IsDigit) && sdt[0] == '0';

	private void btnSend_Click(object sender, EventArgs e)
	{
		if (!SerialPort1.IsOpen)
		{
			MessageBox.Show("Connect COM port trước khi sử dụng lệnh", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		string sdt = txtTarget.Text;
		if (!ValidPhone(sdt))
		{
			MessageBox.Show("Kiểm tra lại định dạng SĐT cần PING (10 số, bắt đầu bằng 0)", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		if (!ckbBao.Checked)
		{
			SendPdu(PduCodec.BuildPingPdu(sdt));
			return;
		}

		string notify = txtNotify.Text;
		if (!ValidPhone(notify))
		{
			MessageBox.Show("Kiểm tra lại định dạng SĐT của bạn (10 số, bắt đầu bằng 0)", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		if (sdt == notify)
		{
			MessageBox.Show("SĐT cần PING trùng SĐT nhận báo. Nhập lại.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}

		var entry = new DAUVAO
		{
			str_CMGS = "",
			sdt_canPING = sdt,
			sdt_canPING_dao = PduCodec.SwapDigits(sdt),
			sdt_trs = "+84" + notify.Substring(1)
		};
		_canBao.Add(entry);
		_strDocKq = "";
		SendPdu(PduCodec.BuildPingPdu(sdt));
		System.Threading.Thread.Sleep(500);
		_pingOk = 0;
		ckbBao.Checked = false;
		TimerPingOk.Enabled = true;
	}

	private void TimerPingOk_Tick(object sender, EventArgs e)
	{
		TimerPingOk.Enabled = false;
		if (_canBao.Count == 0) return;
		_pingOk++;
		var last = _canBao[_canBao.Count - 1];
		string marker = "00B1000B9148" + last.sdt_canPING_dao + "4000AA03201008";
		int pos = _strDocKq.IndexOf(marker, StringComparison.Ordinal);

		if (pos < 0)
		{
			MessageBox.Show("Đã xảy ra lỗi, chưa PING được", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		string tail = _strDocKq.Substring(pos);
		if (tail.Contains("ERROR"))
		{
			MessageBox.Show("Cuộc PING của bạn bị lỗi", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
			_canBao.RemoveAt(_canBao.Count - 1);
		}
		else if (tail.Contains("OK"))
		{
			int cmgsIdx = _strDocKq.IndexOf("CMGS: ", StringComparison.Ordinal);
			if (cmgsIdx >= 0)
			{
				int endIdx = _strDocKq.IndexOf("\r\n", cmgsIdx, StringComparison.Ordinal);
				if (endIdx < 0) endIdx = _strDocKq.Length;
				last.str_CMGS = _strDocKq.Substring(cmgsIdx + 6, endIdx - cmgsIdx - 6);
				_canBao[_canBao.Count - 1] = last;
			}
			TimerChoKq.Enabled = true;
		}
		else if (_pingOk < 5)
		{
			TimerPingOk.Enabled = true;
		}
		else
		{
			MessageBox.Show("Đã xảy ra lỗi nào đó, hãy ghi lại thông tin trong RAW CODE và báo với VTV", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
			_canBao.RemoveAt(_canBao.Count - 1);
		}
	}

	private void TimerChoKq_Tick(object sender, EventArgs e)
	{
		if (_canBao.Count == 0)
		{
			TimerChoKq.Enabled = false;
			return;
		}
		TimerChoKq.Stop();

		if (_strDocKq.Length > 0)
		{
			string catKetQua = "";
			int cdsIdx = _strDocKq.IndexOf("CDS:", StringComparison.Ordinal);
			if (cdsIdx >= 0)
			{
				int i06 = _strDocKq.IndexOf("069148", cdsIdx, StringComparison.Ordinal);
				int i07 = _strDocKq.IndexOf("079148", cdsIdx, StringComparison.Ordinal);
				if (i06 >= 0) catKetQua = _strDocKq.Substring(i06, Math.Min(64, _strDocKq.Length - i06));
				else if (i07 >= 0) catKetQua = _strDocKq.Substring(i07, Math.Min(66, _strDocKq.Length - i07));
			}

			var kq = PduCodec.Decode(catKetQua);
			if (kq.ER)
			{
				var match = _canBao.FirstOrDefault(x => x.str_CMGS == kq.MR);
				if (!string.IsNullOrEmpty(match.str_CMGS))
				{
					SerialPort1.Write("AT+CMGF=1\r\n");
					System.Threading.Thread.Sleep(300);
					SerialPort1.Write($"AT+CMGS=\"{match.sdt_trs}\"\r\n");
					System.Threading.Thread.Sleep(1000);
					SerialPort1.Write($"PING CMGS: {match.str_CMGS}; Den {match.sdt_canPING}; SMSC nhan: {kq.t_ping}; Phat: {kq.t_report}; ket qua: {kq.kq_sms}");
					System.Threading.Thread.Sleep(1000);
					SerialPort1.Write("\u001a");
					System.Threading.Thread.Sleep(3000);
					_canBao.RemoveAll(x => x.sdt_canPING == match.sdt_canPING && x.str_CMGS == match.str_CMGS);
					System.Threading.Thread.Sleep(5000);
					SerialPort1.Write("AT+CMGF=0\r\n");
					System.Threading.Thread.Sleep(300);
				}
				else
				{
					_strDocKq = "";
				}
			}
			else
			{
				_strDocKq = "";
			}
		}

		if (_canBao.Count > 0) TimerChoKq.Start();
	}

	private void btnXoaBao_Click(object sender, EventArgs e)
	{
		_canBao.Clear();
		TimerPingOk.Enabled = false;
		TimerChoKq.Enabled = false;
		ckbBao.Checked = false;
		txtNotify.Text = "";
		_strDocKq = "";
	}

	private void ckbBao_CheckedChanged(object sender, EventArgs e)
	{
		txtNotify.Enabled = ckbBao.Checked;
	}

	// ---------------- Chế độ AT command ----------------

	private void rbPing_CheckedChanged(object sender, EventArgs e)
	{
		panelPing.Visible = rbPing.Checked;
		panelAt.Visible = !rbPing.Checked;
		if (rbPing.Checked) txtTarget.Focus();
	}

	private void rbAt_CheckedChanged(object sender, EventArgs e)
	{
		panelAt.Visible = rbAt.Checked;
		panelPing.Visible = !rbAt.Checked;
		if (rbAt.Checked) txtAt.Focus();
	}

	private void btnSendAt_Click(object sender, EventArgs e)
	{
		if (!SerialPort1.IsOpen)
		{
			MessageBox.Show("Connect COM port trước khi sử dụng lệnh", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		SerialPort1.Write(ckbCr.Checked ? txtAt.Text + "\r" : txtAt.Text);
	}

	private void btnCtrlZ_Click(object sender, EventArgs e)
	{
		if (!SerialPort1.IsOpen)
		{
			MessageBox.Show("Connect COM port trước khi sử dụng lệnh", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		SerialPort1.Write("\u001a");
	}

	// ---------------- Tool ----------------

	private void btnClr_Click(object sender, EventArgs e)
	{
		txtRaw.Clear();
		txtDecode.Clear();
	}

	private void btnDecodeSel_Click(object sender, EventArgs e)
	{
		string selected = txtRaw.SelectedText;
		if (string.IsNullOrWhiteSpace(selected))
		{
			MessageBox.Show("Bạn phải chọn (bôi đen) text cần DECODE", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		selected = selected.Trim().Replace("\r\n", "").Replace("\r", "").Replace("\n", "");
		var kq = PduCodec.Decode(selected);
		if (!kq.ER)
		{
			MessageBox.Show("Chỉ chọn phần kết quả REPORT của lệnh PING để DECODE", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		txtDecode.AppendText($"PING SMS có CMGS: {kq.MR}\r\nĐến SĐT {kq.sdt_dcping}\r\nĐược SMSC nhận lúc: {kq.t_ping}, phát lúc: {kq.t_report}\r\nCó kết quả: {kq.kq}\r\n\r\n");
	}

	private void btn_TK_Click(object sender, EventArgs e)
	{
		if (!SerialPort1.IsOpen)
		{
			MessageBox.Show("Connect COM port trước khi sử dụng lệnh", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		SerialPort1.Write("AT+CMGF=1\r\n");
		System.Threading.Thread.Sleep(200);
		SerialPort1.Write("AT+CUSD=1,\"*101#\"\r\n");
		System.Threading.Thread.Sleep(200);
		SerialPort1.Write("AT+CMGF=0\r\n");
	}

	private void btn_chkHard_Click(object sender, EventArgs e)
	{
		if (!SerialPort1.IsOpen)
		{
			MessageBox.Show("Connect COM port trước khi sử dụng lệnh", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		SerialPort1.Write("AT\r\n");
		System.Threading.Thread.Sleep(200);
		SerialPort1.Write("AT+CSQ\r\n");
	}

	private void btnHelp_Click(object sender, EventArgs e)
	{
		MessageBox.Show("- Bạn có thể PING cho một hoặc nhiều số ĐT đang tắt máy.\r\n" +
			"Thiết bị sẽ SMS báo cho bạn khi SĐT đó online trở lại.\r\n" +
			"- Trong khi chờ SMS báo hiệu, không được tắt PC/ứng dụng, không để PC sleep.\r\n" +
			"- Trong khi chờ, vẫn có thể PING SĐT khác hoặc dùng AT command bình thường.", "Trợ giúp");
	}

	private void btnAbout_Click(object sender, EventArgs e)
	{
		MessageBox.Show("Thiết bị do VTV và PTH thực hiện.\r\nPhiên bản viết lại gọn từ mã gốc.", "About");
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		try { if (SerialPort1.IsOpen) SerialPort1.Close(); } catch { /* ignore */ }
		base.OnFormClosing(e);
	}
}
