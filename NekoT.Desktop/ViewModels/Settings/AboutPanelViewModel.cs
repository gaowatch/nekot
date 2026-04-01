using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NekoT.Desktop.ViewModels.Settings
{
    public class AboutPanelViewModel : ViewModelBase
    {
        private string _version = "v0.1.0";
        private string _author = "NekoT Team";
        private string _license = "Apache 2.0";
        private string _website = "https://github.com/gaowatch/nekot";

        public string Version { get => _version; set => SetField(ref _version, value); }
        public string Author { get => _author; set => SetField(ref _author, value); }
        public string License { get => _license; set => SetField(ref _license, value); }
        public string Website { get => _website; set => SetField(ref _website, value); }
    }
}
