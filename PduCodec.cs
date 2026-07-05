using System;
using System.Collections.Generic;

namespace SmsPing;

/// <summary>
/// Mã hóa/giải mã PDU dùng cho tính năng PING SMS.
/// Port 1-1 từ hàm senPDU/Decode gốc, chỉ gọn lại bằng bảng tra thay cho if-else dài.
/// </summary>
internal static class PduCodec
{
	/// <summary>Đảo cặp số theo chuẩn semi-octet (giữ nguyên thuật toán gốc).</summary>
	public static string SwapDigits(string sdt)
	{
		return "" + sdt[2] + sdt[1] + sdt[4] + sdt[3] + sdt[6] + sdt[5] + sdt[8] + sdt[7] + "F" + sdt[9];
	}

	/// <summary>Tạo PDU class-0 "ping" gửi tới <paramref name="sdt"/> (10 số, bắt đầu bằng 0).</summary>
	public static string BuildPingPdu(string sdt)
	{
		return "0071000B9148" + SwapDigits(sdt) + "000800050401020000";
	}

	/// <summary>Cặp (thông báo tiếng Việt, thông báo rút gọn gửi SMS) cho một mã kết quả.</summary>
	private sealed class KqEntry
	{
		public readonly string Vi;
		public readonly string Sms;
		public KqEntry(string vi, string sms) { Vi = vi; Sms = sms; }
	}

	private static readonly Dictionary<string, KqEntry> KqTable = new Dictionary<string, KqEntry>
	{
		["00"] = new KqEntry("Số điện thoại mà bạn PING đang ONLINE.", "SDT PING ONLINE."),
		["01"] = new KqEntry("SMS đã gửi tới SĐT đích n SMSC không thể xác nhận việc phát.", "SMSC can not send."),
		["02"] = new KqEntry("SMS được thay thế bởi SMSC.", "SMS replace SMSC."),
		["03"] = new KqEntry("Lower End of the Reserved Values in This Sector.", "Lower End of the Reserved Values in This Sector."),
		["0F"] = new KqEntry("High End of the Reserved Values in This Sector.", "High End of the Reserved Values in This Sector."),
		["10"] = new KqEntry("Lower End of Values Specific to each SMSC.", "Lower End of Values Specific to each SMSC."),
		["1F"] = new KqEntry("High End of Values Specific to each SMSC in This Sector.", "High End of Values Specific to each SMSC in This Sector."),
		["20"] = new KqEntry("Congestion.", "Congestion."),
		["60"] = new KqEntry("Congestion.", "Congestion."),
		["21"] = new KqEntry("ĐT đích bận.", "SDT ban."),
		["61"] = new KqEntry("ĐT đích bận.", "SDT ban."),
		["22"] = new KqEntry("Không hồi đáp ĐT đích.", "SDT Khong hoi dap."),
		["62"] = new KqEntry("Không hồi đáp ĐT đích.", "SDT Khong hoi dap."),
		["23"] = new KqEntry("Service rejected.", "Service rejected."),
		["63"] = new KqEntry("Service rejected.", "Service rejected."),
		["24"] = new KqEntry("service not available.", "service not available."),
		["64"] = new KqEntry("service not available.", "service not available."),
		["25"] = new KqEntry("Lỗi ở ĐT đích.", "Loi o DT dich."),
		["65"] = new KqEntry("Lỗi ở ĐT đích.", "Loi o DT dich."),
		["26"] = new KqEntry("Lower End of the Reserved Values in This Sector.", "Lower End of the Reserved Values in This Sector."),
		["66"] = new KqEntry("Lower End of the Reserved Values in This Sector.", "Lower End of the Reserved Values in This Sector."),
		["2F"] = new KqEntry("High End of the Reserved Values in This Sector.", "High End of the Reserved Values in This Sector."),
		["6F"] = new KqEntry("High End of the Reserved Values in This Sector.", "High End of the Reserved Values in This Sector."),
		["30"] = new KqEntry("Lower End of Values Specific to each SMSC.", "Lower End of Values Specific to each SMSC."),
		["70"] = new KqEntry("Lower End of Values Specific to each SMSC.", "Lower End of Values Specific to each SMSC."),
		["3F"] = new KqEntry("High End of Values Specific to each SMSC in This Sector.", "High End of Values Specific to each SMSC in This Sector."),
		["7F"] = new KqEntry("High End of Values Specific to each SMSC in This Sector.", "High End of Values Specific to each SMSC in This Sector."),
		["40"] = new KqEntry("Remote procedure error.", "Remote procedure error."),
		["41"] = new KqEntry("Incompatible destination.", "Incompatible destination."),
		["42"] = new KqEntry("Connection rejected by ĐT đích.", "Connection rejected by DT dich."),
		["43"] = new KqEntry("Not obtainable.", "Not obtainable."),
		["44"] = new KqEntry("Quality of service not available.", "Quality of service not available."),
		["45"] = new KqEntry("Số điện thoại KHÔNG CÓ THỰC.", "SDT PING KHONG CO THUC."),
		["46"] = new KqEntry("Đã hết hạn gửi SMS. SMS Center đã xóa tin nhắn.", "Het han. SMS xoa TN"),
		["47"] = new KqEntry("SMS Deleted by originating ĐT đích.", "SMS Deleted by originating DT dich."),
		["48"] = new KqEntry("SMS Deleted by SMSC Administration.", "SMS Deleted by SMSC Administration."),
		["49"] = new KqEntry("SMS does not exist.", "SMS does not exist."),
		["4A"] = new KqEntry("Lower End of the Reserved Values in This Sector.", "Lower End of the Reserved Values in This Sector."),
		["4F"] = new KqEntry("High End of the Reserved Values in This Sector.", "High End of the Reserved Values in This Sector."),
		["50"] = new KqEntry("Lower End of Values Specific to each SMSC.", "Lower End of Values Specific to each SMSC."),
		["5F"] = new KqEntry("High End of Values Specific to each SMSC in This Sector.", "High End of Values Specific to each SMSC in This Sector."),
	};

	private static string Hx(string s, int i) => "" + s[i] + s[i + 1];

	/// <summary>Giải mã bản tin report (CDS). Port 1-1 từ hàm Decode() gốc.</summary>
	public static KETQUA Decode(string textCode)
	{
		KETQUA r = default;
		switch (textCode?.Length ?? 0)
		{
			case 66:
				r.ER = true;
				r.MR = Convert.ToInt32(Hx(textCode, 18), 16).ToString();
				r.sdt_dcping = "0" + textCode[27] + textCode[26] + textCode[29] + textCode[28] + textCode[31] + textCode[30] + textCode[33] + textCode[32] + textCode[35];
				r.t_ping = "" + textCode[43] + textCode[42] + ":" + textCode[45] + textCode[44] + ":" + textCode[47] + textCode[46] + ", ngay " + textCode[41] + textCode[40] + "/" + textCode[39] + textCode[38] + "/20" + textCode[37] + textCode[36];
				r.t_report = "" + textCode[57] + textCode[56] + ":" + textCode[59] + textCode[58] + ":" + textCode[61] + textCode[60] + ", ngay " + textCode[55] + textCode[54] + "/" + textCode[53] + textCode[52] + "/20" + textCode[51] + textCode[50];
				r.kq = Hx(textCode, 64);
				r.kq_sms = r.kq;
				break;
			case 64:
				r.ER = true;
				r.MR = Convert.ToInt32(Hx(textCode, 16), 16).ToString();
				r.sdt_dcping = "0" + textCode[25] + textCode[24] + textCode[27] + textCode[26] + textCode[29] + textCode[28] + textCode[31] + textCode[30] + textCode[33];
				r.t_ping = "" + textCode[41] + textCode[40] + ":" + textCode[43] + textCode[42] + ":" + textCode[45] + textCode[44] + ", ngay " + textCode[39] + textCode[38] + "/" + textCode[37] + textCode[36] + "/20" + textCode[35] + textCode[34];
				r.t_report = "" + textCode[55] + textCode[54] + ":" + textCode[57] + textCode[56] + ":" + textCode[59] + textCode[58] + ", ngay " + textCode[53] + textCode[52] + "/" + textCode[51] + textCode[50] + "/20" + textCode[49] + textCode[48];
				r.kq = Hx(textCode, 62);
				r.kq_sms = r.kq;
				break;
			case 52:
				r.ER = true;
				r.MR = Convert.ToInt32(Hx(textCode, 4), 16).ToString();
				r.sdt_dcping = "0" + textCode[13] + textCode[12] + textCode[15] + textCode[14] + textCode[17] + textCode[16] + textCode[19] + textCode[18] + textCode[21];
				r.t_ping = "" + textCode[29] + textCode[28] + ":" + textCode[31] + textCode[30] + ":" + textCode[33] + textCode[32] + ", ngay " + textCode[27] + textCode[26] + "/" + textCode[25] + textCode[24] + "/20" + textCode[23] + textCode[22];
				r.t_report = "" + textCode[43] + textCode[42] + ":" + textCode[45] + textCode[44] + ":" + textCode[47] + textCode[46] + ", ngay " + textCode[40] + textCode[39] + "/" + textCode[38] + textCode[37] + "/20" + textCode[36] + textCode[35];
				r.kq = Hx(textCode, 50);
				r.kq_sms = r.kq;
				break;
			case 54:
				r.ER = true;
				r.MR = Convert.ToInt32(Hx(textCode, 4), 16).ToString();
				r.sdt_dcping = "0" + textCode[13] + textCode[12] + textCode[15] + textCode[14] + textCode[17] + textCode[16] + textCode[19] + textCode[18] + textCode[21];
				r.t_ping = "" + textCode[29] + textCode[28] + ":" + textCode[31] + textCode[30] + ":" + textCode[33] + textCode[32] + ", ngay " + textCode[27] + textCode[26] + "/" + textCode[25] + textCode[24] + "/20" + textCode[23] + textCode[22];
				r.t_report = "" + textCode[43] + textCode[42] + ":" + textCode[45] + textCode[44] + ":" + textCode[47] + textCode[46] + ", ngay " + textCode[41] + textCode[40] + "/" + textCode[39] + textCode[38] + "/20" + textCode[37] + textCode[36];
				r.kq = Hx(textCode, 52);
				r.kq_sms = r.kq;
				break;
			default:
				r.ER = false;
				r.MR = "";
				r.sdt_dcping = "";
				r.t_ping = "";
				r.t_report = "";
				r.kq = "";
				r.kq_sms = "";
				break;
		}
		if (r.kq != null && KqTable.TryGetValue(r.kq, out KqEntry mapped))
		{
			r.kq = mapped.Vi;
			r.kq_sms = mapped.Sms;
		}
		return r;
	}
}
