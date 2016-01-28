using Kms2.Crypto.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
//

namespace MacGenerator
{
    public partial class Main : Form
    {
        ToolTip tt;
        Stopwatch timer;
        public Main()
        {
            InitializeComponent();
            tt = new ToolTip();
            timer = new Stopwatch();
        }

        private string GenMac(IMsgUtility msgUtility,string data,int macLength = 8)
        {
            byte[] result = Encoding.ASCII.GetBytes(data);
            result = result.Concat(new byte[macLength]).ToArray();//insert a byte[ mac length ]
            string readerId = msgUtility.GetStr(result, "ReaderId").Substring(0, 16);
            string transDatetime = msgUtility.GetStr(result, "TransDateTime");
            // fetch sha1 data
            byte[] hashDataBytes = msgUtility.GetBytes(result, "HeaderVersion", "Mac");
            byte[] sha1Result = MsgContainer.HashWorker.ComputeHash(hashDataBytes);
            string macStr = MsgContainer.Mac2Manager.GetAuthMac(readerId, transDatetime, sha1Result);
            return macStr;
        }

        private string GetHeader(GroupBox container,int count)
        {
            string[] headers = new string[count];
            string result = null;
            TextBox tmp = null;
            for (int i = 0; i < container.Controls.Count; i++)
            {
                if (container.Controls[i] is TextBox)
                {
                    tmp = container.Controls[i] as TextBox;
                    //控制向定位順序來當排序依據//有變要改
                    headers[tmp.TabIndex] = tmp.Text;
                }
            }
            result = string.Concat(headers);

            return result;
        }

        private string GetData(GroupBox container, int count)
        {
            string[] datas = new string[count];
            string result = null;
            TextBox tmp = null;
            for (int i = 0; i < container.Controls.Count; i++)
            {
                if (container.Controls[i] is TextBox)
                {
                    tmp = container.Controls[i] as TextBox;
                    if (tmp.Name.ToUpper().Contains("MAC"))
                        continue;
                    switch (tmp.TabIndex)
                    {
                        case 8:
                            //控制向定位順序來當排序依據//有變要改
                            datas[tmp.TabIndex] = "\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000\u0000";
                            break;
                        default:
                            //控制向定位順序來當排序依據//有變要改
                            datas[tmp.TabIndex] = tmp.Text;
                            break;
                     }
                }
            }
            result = string.Concat(datas);

            return result;
        }

        private string asciiString2hexString(string asciiStr)
        {
            byte[] data = Encoding.ASCII.GetBytes(asciiStr);

            return BitConverter.ToString(data).Replace("-", "");
        }

        private void HexTextBoxTooltip_MouseEnter(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tt.Show(String.Format("這是Hex的 {0} Request電文",(string)tb.Tag), tb, 5000);
        }

        private void GenPurchaseReturn_Click(object sender, EventArgs e)
        {
            int headerCount = 20;
            int dataCount = 14;

            string headerStr = GetHeader(this.PurchaseReturnHeader, headerCount);
            string dataStr = GetData(this.PurchaseReturnData, dataCount);
            //string headerHexStr = asciiString2hexString(headerStr);
            //string dataHexStr = asciiString2hexString(dataStr);
            string makeMACString = headerStr + dataStr;
            this.PurchaseReturnMAC.Text = GenMac(MsgContainer.PRReqMsgUtility, makeMACString);
            string msgStr = makeMACString + this.PurchaseReturnMAC.Text;
            this.PurchaseReturnHexString.Text = this.asciiString2hexString(msgStr);
        }

        private void GenLoad_Click(object sender, EventArgs e)
        {
            int headerCount = 20;
            int dataCount = 14;

            string headerStr = GetHeader(this.LoadHeader, headerCount);
            string dataStr = GetData(this.LoadData, dataCount);
            string headerHexStr = asciiString2hexString(headerStr);
            string dataHexStr = asciiString2hexString(dataStr);
            string makeMACString = headerStr + dataStr;
            this.LoadMAC.Text = GenMac(MsgContainer.LOLReqMsgUtility, makeMACString);
            string msgStr = makeMACString + this.LoadMAC.Text;
            this.LoadHexString.Text = this.asciiString2hexString(msgStr);
            //MessageBox.Show("[Header]ASCII:" + headerStr);
            //MessageBox.Show("[Header]Hex:" + headerHexStr);
            //MessageBox.Show("[Data]ASCII:" + dataStr);
            //MessageBox.Show("[Data]Hex:" + dataHexStr);
        }

        private void GenAutoLoad_Click(object sender, EventArgs e)
        {
            int headerCount = 20;//header項數
            int dataCount = 16;//data項數

            string headerStr = GetHeader(this.AutoLoadHeader, headerCount);
            string dataStr = GetData(this.AutoLoadData, dataCount);
            string headerHexStr = asciiString2hexString(headerStr);
            string dataHexStr = asciiString2hexString(dataStr);
            string makeMACString = headerStr + dataStr;
            this.AutoLoadMAC.Text = GenMac(MsgContainer.AOLReqMsgUtility, makeMACString);
            string msgStr = makeMACString + this.AutoLoadMAC.Text;
            this.AutoLoadHexString.Text = this.asciiString2hexString(msgStr);
        }

        private void GenAutoLoadQuery_Click(object sender, EventArgs e)
        {
            int headerCount = 20;//header項數
            int dataCount = 9;//data項數

            string headerStr = GetHeader(this.AutoLoadQueryHeader, headerCount);
            string dataStr = GetData(this.AutoLoadQueryData, dataCount);
            string headerHexStr = asciiString2hexString(headerStr);
            string dataHexStr = asciiString2hexString(dataStr);
            string makeMACString = headerStr + dataStr;
            this.AutoLoadQueryMAC.Text = GenMac(MsgContainer.ALQReqMsgUtility, makeMACString);
            string msgStr = makeMACString + this.AutoLoadQueryMAC.Text;
            this.AutoLoadQueryHexString.Text = this.asciiString2hexString(msgStr);
        }

        private void CardEnabled_Tooltip(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tt.Show(String.Format("{0}", (string)tb.Tag), tb, 5000);
        }

        private void SendRequest_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            btn.Enabled = false;
            this.Invoke(new UpDateResponse(StartConmunicate));
            
        }
        delegate void UpDateResponse();
        private void StartConmunicate()
        {
            string ip = this.IP.Text;
            int port = (int)this.Port.Value;
            byte[] receiveData = null;
            byte[] sendData = this.StringToByteArray(this.RequestHex.Text);
            try
            {
                using (SocketClient.Domain.SocketClient client = new SocketClient.Domain.SocketClient(ip, port))
                {
                    timer.Restart();
                    if (client.ConnectToServer())
                    {
                        receiveData = client.SendAndReceive(sendData);
                        string result = String.Format("Receive Data(byte length:{0}): {1}", receiveData.Length, BitConverter.ToString(receiveData).Replace("-", ""));
                        this.ResponseHex.Text = result;
                        this.ReturnCode.Text = MsgContainer.ALQRespMsgUtility.GetStr(receiveData, "ReturnCode");
                    }
                }
            }
            catch (SocketException sckEx)
            {
                MessageBox.Show(String.Format("Socket 異常:{0}", sckEx.Message));
                this.ResponseHex.Text = string.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("一般異常:{0}", ex.Message));
                this.ResponseHex.Text = string.Empty;
            }
            finally
            {
                timer.Stop();
                this.TimeSpend.Text = this.timer.ElapsedMilliseconds.ToString();
                this.SendRequest.Enabled = true;
            }
        }

        private byte[] StringToByteArray(String hex)
        {
            //http://stackoverflow.com/questions/1038031/what-is-the-easiest-way-in-c-sharp-to-trim-a-newline-off-of-a-string
            hex = hex.TrimEnd('\r', '\n');//trim CRLF
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        private void ClearResponse(object sender, EventArgs e)
        {
            this.ResponseHex.Text = string.Empty;
            this.ReturnCode.Text = string.Empty;
            this.TimeSpend.Text = string.Empty;
        }
    }
}
