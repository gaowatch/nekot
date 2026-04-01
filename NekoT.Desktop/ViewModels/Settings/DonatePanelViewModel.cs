using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NekoT.Desktop.ViewModels.Settings
{
    public class DonatePanelViewModel : ViewModelBase
    {
        private string _alipayAccount = "";
        private string _wechatAccount = "";
        private string _paypalLink = "";

        public string AlipayAccount { get => _alipayAccount; set => SetField(ref _alipayAccount, value); }
        public string WechatAccount { get => _wechatAccount; set => SetField(ref _wechatAccount, value); }
        public string PayPalLink { get => _paypalLink; set => SetField(ref _paypalLink, value); }
    }
}
