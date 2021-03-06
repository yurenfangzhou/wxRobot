﻿using CCWin;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using wxRobot.Util.Enums;
using wxRobot.Model;
using wxRobot.Model.Dto;
using wxRobot.Services;
using wxRobot.Https;
using wxRobot.Util.Utils;
using Newtonsoft.Json;

namespace wxRobot
{
    public partial class FormMain : CCSkinMain
    {
        private const String DEFAULT_TEXT = "请输入你要发送的信息";

        private static List<object> _contact_all = new List<object>();
        private static List<WXUser> contact_all = new List<WXUser>();

        /// <summary>
        /// 当前登录微信用户
        /// </summary>
        private static WXUser _me;

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            WindowInit();
        }



        private void WindowInit()
        {
            skinTabControl1.TabPages[1].Select();
            //扫码
            GetLoginQRCode();
            BindMessageGrid();
        }

        public void IsAuth()
        {
            ServiceRecordSvc recordSvc = new ServiceRecordSvc();
            var OperResult = recordSvc.IsAuth();
            if (OperResult.Code == ResultCodeEnums.Auth)
            {
                GetLoginQRCode();
            }
            else if (OperResult.Code == ResultCodeEnums.AuthExpire)
            {
                var result = MessageBox.Show(OperResult.Msg + "，是否现在进行授权", "系统提示", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    AuthForm authForm = new AuthForm();
                    if (authForm.ShowDialog() == DialogResult.OK)
                    {
                        GetLoginQRCode();
                    }
                }
                else
                {
                    Application.Exit();
                }
            }
            else if (OperResult.Code == ResultCodeEnums.UnAuth)
            {
                var result = MessageBox.Show(OperResult.Msg + "，是否现在进行授权", "系统提示", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    AuthForm authForm = new AuthForm();
                    if (authForm.ShowDialog() == DialogResult.OK)
                    {
                        GetLoginQRCode();
                    }
                }
                else
                {
                    Application.Exit();
                }
            }
        }

        public void BindMessageGrid()
        {
            MessageServces servces = new MessageServces();
            this.DataGridMessage.DataSource = servces.GetDefultMessage();
        }

        public void GetLoginQRCode()
        {
            picQRCode.Image = null;
            picQRCode.SizeMode = PictureBoxSizeMode.Zoom;
            lblTip.Text = "手机微信扫一扫以登录";
            ((Action)(delegate ()
            {
                //异步加载二维码
                LoginService ls = new LoginService();
                Image qrcode = ls.GetQRCode();
                if (qrcode != null)
                {
                    this.BeginInvoke((Action)delegate ()
                    {
                        picQRCode.Image = qrcode;
                    });
                    //ServiceRecordSvc recordSvc = new ServiceRecordSvc();
                    //recordSvc.SetRecord();
                    object login_result = null;
                    while (true)  //循环判断手机扫面二维码结果
                    {
                        login_result = ls.LoginCheck();
                        if (login_result is Image) //已扫描 未登录
                        {
                            this.BeginInvoke((Action)delegate ()
                            {
                                lblTip.Text = "请点击手机上登录按钮";
                                picQRCode.SizeMode = PictureBoxSizeMode.CenterImage;  //显示头像
                                picQRCode.Image = login_result as Image;
                            });
                        }
                        if (login_result is string)  //已完成登录
                        {
                            this.BeginInvoke((Action)delegate ()
                            {
                                lblTip.Text = "登录成功！";
                            });

                            //访问登录跳转URL
                            ls.GetSidUid(login_result as string);
                            //初始化API
                            HttpApi api = new HttpApi();
                            api.InitApi(login_result.ToString());
                            //获取好友和并绑定
                            UserServices userServices = new UserServices();
                            WXServices wxServices = new WXServices();
                            JObject initResult = wxServices.WxInit();
                            if (initResult != null)
                            {
                                _me = new WXUser();
                                _me.UserName = initResult["User"]["UserName"].ToString();
                                _me.City = "";
                                _me.HeadImgUrl = initResult["User"]["HeadImgUrl"].ToString();
                                _me.NickName = initResult["User"]["NickName"].ToString();
                                _me.Province = "";
                                _me.PYQuanPin = initResult["User"]["PYQuanPin"].ToString();
                                _me.RemarkName = initResult["User"]["RemarkName"].ToString();
                                _me.RemarkPYQuanPin = initResult["User"]["RemarkPYQuanPin"].ToString();
                                _me.Sex = initResult["User"]["Sex"].ToString();
                                _me.Signature = initResult["User"]["Signature"].ToString();
                            }

                            JObject contact_result = userServices.GetContact(); //通讯录
                            if (contact_result != null)
                            {
                                foreach (JObject contact in contact_result["MemberList"])  //完整好友名单
                                {
                                    WXUser user = new WXUser();
                                    user.UserName = contact["UserName"].ToString();
                                    user.City = contact["City"].ToString();
                                    user.HeadImgUrl = contact["HeadImgUrl"].ToString();
                                    user.NickName = contact["NickName"].ToString();
                                    user.Province = contact["Province"].ToString();
                                    user.PYQuanPin = contact["PYQuanPin"].ToString();
                                    user.RemarkName = contact["RemarkName"].ToString();
                                    user.RemarkPYQuanPin = contact["RemarkPYQuanPin"].ToString();
                                    user.Sex = contact["Sex"].ToString();
                                    user.Signature = contact["Signature"].ToString();
                                    contact_all.Add(user);
                                }
                            }
                            IOrderedEnumerable<WXUser> list_all = contact_all.OrderBy(e => (e as WXUser).ShowPinYin);

                            WXUser wx;
                            string start_char;
                            foreach (object o in list_all)
                            {
                                wx = o as WXUser;
                                start_char = wx.ShowPinYin == "" ? "" : wx.ShowPinYin.Substring(0, 1);
                                if (!_contact_all.Contains(start_char.ToUpper()))
                                {
                                    _contact_all.Add(start_char.ToUpper());
                                }
                                _contact_all.Add(o);
                            }
                            //等待结束
                            this.BeginInvoke((Action)(delegate ()
                            {
                                //通讯录
                                wFriendsList1.Items.AddRange(_contact_all.ToArray());
                                BindOwer(_me);
                            }));
                            return;
                        }
                    }
                }
            })).BeginInvoke(null, null);
        }

        public void BindOwer(WXUser me)
        {
            picImage.Image = me.Icon;
            lblNick.Text = me.NickName;
            lblArea.Text = me.City + "，" + me.Province;
            lblSignature.Text = me.Signature;
            picSexImage.Image = me.Sex == "1" ? Properties.Resources.male : Properties.Resources.female;
            picSexImage.Location = new Point(lblNick.Location.X + lblNick.Width + 4, picSexImage.Location.Y);
            if (me.Icon == null)
            {
                picImage.Image = picQRCode.Image;
            }
            else
            {
                picImage.Image = me.Icon;
            }
        }

        private void skinButton1_Click(object sender, EventArgs e)
        {
            this.skinButton1.Enabled = false;
            List<MessageType> message = new List<MessageType>();
            int count = DataGridMessage.Rows.Count;
            for (int i = 0; i < count; i++)
            {
                DataGridViewCheckBoxCell checkCell = (DataGridViewCheckBoxCell)DataGridMessage.Rows[i].Cells[0];
                Boolean flag = Convert.ToBoolean(checkCell.Value);
                if (flag == true)
                {
                    MessageType msgType = new MessageType()
                    {
                        SendType = this.DataGridMessage.Rows[i].Cells[1].Value.ToString(),
                        TxtContent = this.DataGridMessage.Rows[i].Cells[2].Value.ToString()
                    };
                    message.Add(msgType);
                }
            }
            if (message.Count <= 0)
            {
                MessageBox.Show("请选择好你要发送的消息！");
                return;
            }
            WXMesssage msg = new WXMesssage();
            //发消息
            var sendMsg = message.Where(a => a.SendType == "文本").FirstOrDefault();
            if (null != sendMsg)
            {

                foreach (var item in contact_all)
                {
                    msg.From = _me.UserName;
                    msg.Readed = false;
                    msg.To = item.UserName;
                    msg.Time = DateTime.Now;
                    msg.Type = 1;
                    msg.Msg = sendMsg.TxtContent;
                    _me.SendMsg(msg);
                    outPost(item.NickName, sendMsg.SendType);
                }
            }
            //发图片
            var sendImage = message.Where(a => a.SendType == "图片").FirstOrDefault();
            if (null != sendImage)
            {
                if (!File.Exists(sendImage.TxtContent))
                {
                    MessageBox.Show("文件不存在，请选择好文件！");
                    return;
                }
                //先上传
                WXServices wxServices = new WXServices();
                var resultJson = wxServices.UploadImage(sendImage.TxtContent);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    JObject obj = JsonConvert.DeserializeObject(resultJson) as JObject;
                    string mediaId = obj["MediaId"].ToString();
                    if (!string.IsNullOrEmpty(mediaId))
                    {
                        foreach (var item in contact_all)
                        {
                            msg.From = _me.UserName;
                            msg.Readed = false;
                            msg.To = item.UserName;
                            msg.Time = DateTime.Now;
                            msg.MediaId = mediaId;
                            _me.SendImage(msg);
                            outPost(item.NickName, sendImage.SendType);
                        }
                    }
                }
            }
            //发视频
            var sendVideo = message.Where(a => a.SendType == "视频").FirstOrDefault();
            if (null != sendVideo)
            {
                if (!File.Exists(sendVideo.TxtContent))
                {
                    MessageBox.Show("文件不存在，请选择好文件！");
                    return;
                }
                WXServices wxServices = new WXServices();
                var resultJson = wxServices.UploadVideo(sendVideo.TxtContent, _me.UserName, contact_all[0].UserName);
                if (!string.IsNullOrEmpty(resultJson))
                {
                    JObject obj = JsonConvert.DeserializeObject(resultJson) as JObject;
                    string mediaId = obj["MediaId"].ToString();
                    if (!string.IsNullOrEmpty(mediaId))
                    {
                        foreach (var item in contact_all)
                        {
                            msg.From = _me.UserName;
                            msg.Readed = false;
                            msg.To = item.UserName;
                            msg.Time = DateTime.Now;
                            msg.MediaId = mediaId;
                            _me.SendVideo(msg);
                            outPost(item.NickName, sendVideo.SendType);
                        }
                    }
                }
            }
            this.skinButton1.Enabled = true;
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("你确定要关闭吗！", "提示信息", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                e.Cancel = false;  //点击OK
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            // IsAuth();
        }

        private void DataGridMessage_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == 1 && e.ColumnIndex == 2)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "选择图片文件";
                ofd.ShowHelp = true;
                ofd.Filter = "图片(*.jpg)|*.jpg|图片(*.jpge)|*.jpge|图片(*.gif)|*.gif";//过滤格式
                ofd.Multiselect = false;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    this.DataGridMessage.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = ofd.FileName;
                }
            }
            if (e.RowIndex == 2 && e.ColumnIndex == 2)
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "选择视频文件";
                ofd.ShowHelp = true;
                ofd.Filter = "视频(*.mp4)|*.mp4)";//过滤格式
                ofd.Multiselect = false;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    this.DataGridMessage.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = ofd.FileName;
                }
            }
        }

        private void outPost(string toUSerName, string msgType)
        {
            var txt = txtLog.Text;
            txtLog.Text = string.Format("{0}\t已发{1}信息给{2}\r\n{3}", DateTime.Now.ToString(), toUSerName, msgType, txt);
        }
    }
}
