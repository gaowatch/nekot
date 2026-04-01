using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NekoT.Desktop.ViewModels.Settings
{
    public class GeneralPanelViewModel : ViewModelBase
    {
        private string _language = "zh-CN";
        private bool _enableStealthMode = true;
        private bool _disableDevTools = false;
        private bool _blockTracking = true;
        private bool _blockAds = true;

        public string Language { get => _language; set => SetField(ref _language, value); }
        public bool EnableStealthMode { get => _enableStealthMode; set => SetField(ref _enableStealthMode, value); }
        public bool DisableDevTools { get => _disableDevTools; set => SetField(ref _disableDevTools, value); }
        public bool BlockTracking { get => _blockTracking; set => SetField(ref _blockTracking, value); }
        public bool BlockAds { get => _blockAds; set => SetField(ref _blockAds, value); }
    }
}
