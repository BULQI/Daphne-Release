﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.1026
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DaphneGui.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "10.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string lastOpenScenario {
            get {
                return ((string)(this["lastOpenScenario"]));
            }
            set {
                this["lastOpenScenario"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool autoZoomFit {
            get {
                return ((bool)(this["autoZoomFit"]));
            }
            set {
                this["autoZoomFit"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.SpecialSettingAttribute(global::System.Configuration.SpecialSetting.ConnectionString)]
        [global::System.Configuration.DefaultSettingValueAttribute("Data Source=.\\SQLEXPRESS;AttachDbFilename=|DataDirectory|\\Databases\\Msi.mdf;Integ" +
            "rated Security=True;User Instance=True")]
        public string MsiConnectionString {
            get {
                return ((string)(this["MsiConnectionString"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool skipDataBaseWrites {
            get {
                return ((bool)(this["skipDataBaseWrites"]));
            }
            set {
                this["skipDataBaseWrites"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool runStatisticalSummary {
            get {
                return ((bool)(this["runStatisticalSummary"]));
            }
            set {
                this["runStatisticalSummary"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool suggestExpNameChange {
            get {
                return ((bool)(this["suggestExpNameChange"]));
            }
            set {
                this["suggestExpNameChange"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool writeCellsummaries {
            get {
                return ((bool)(this["writeCellsummaries"]));
            }
            set {
                this["writeCellsummaries"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool bufferDataBaseWriting {
            get {
                return ((bool)(this["bufferDataBaseWriting"]));
            }
            set {
                this["bufferDataBaseWriting"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1000000000")]
        public uint dataBaseBufferMaxRows {
            get {
                return ((uint)(this["dataBaseBufferMaxRows"]));
            }
            set {
                this["dataBaseBufferMaxRows"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10000")]
        public uint dataBaseBufferMinRows {
            get {
                return ((uint)(this["dataBaseBufferMinRows"]));
            }
            set {
                this["dataBaseBufferMinRows"] = value;
            }
        }
    }
}
