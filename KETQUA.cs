namespace VTV_SMS_Ping;

/// <summary>Kết quả decode một bản tin report (CDS) trả về từ SMSC.</summary>
internal struct KETQUA
{
    public bool ER;
    public string MR;
    public string sdt_dcping;
    public string t_ping;
    public string t_report;
    public string kq;
    public string kq_sms;
}
