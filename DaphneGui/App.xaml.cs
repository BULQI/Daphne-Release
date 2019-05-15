/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Abt.Controls.SciChart.Visuals;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            //This is for distributing SciChart License
            SciChartSurface.SetRuntimeLicenseKey(@"<LicenseContract>
                <Customer>Boston University</Customer>
                <OrderId>ABT150602-7817-17129</OrderId>
                <LicenseCount>2</LicenseCount>
                <IsTrialLicense>false</IsTrialLicense>
                <SupportExpires>06/01/2016 00:00:00</SupportExpires>
                <ProductCode>SC-WPF-PRO</ProductCode>
                <KeyCode>lwABAAEAAAABP0afTJ3QAQIAbgBDdXN0b21lcj1Cb3N0b24gVW5pdmVyc2l0eTtPcmRlcklkPUFCVDE1MDYwMi03ODE3LTE3MTI5O1N1YnNjcmlwdGlvblZhbGlkVG89MDEtSnVuLTIwMTY7UHJvZHVjdENvZGU9U0MtV1BGLVBST014t+nKROLNtGdhpjP6LGe/Z4sFztF5TXE18PQ7IiTvzgGt9+KfAjsLcG3mFQaxXg==</KeyCode>
            </LicenseContract>");
        }

        void App_Deactivated(object sender, EventArgs e)
        {            
            // Application deactivated - close open items that don't automatically
            MainWindow mw = (DaphneGui.MainWindow)this.MainWindow;
            mw.CellOptionsExpander.IsExpanded = false;
            mw.ECMOptionsExpander.IsExpanded = false;
        }
    }
}
