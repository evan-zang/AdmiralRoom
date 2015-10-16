﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Huoyaoyuan.AdmiralRoom.Officer
{
    public struct Modernizable
    {
        public int Default { get; private set; }
        public int Max { get; private set; }
        public int Current => Default + Upgrated;
        public int Upgrated { get; private set; }
        public int ShowValue { get; private set; }
        public int Upward => Max - Current;
        public bool IsMax => Current >= Max;
        public Modernizable(LimitedValue masterdata,int upgradedata,int showdata)
        {
            Default = masterdata.Current;
            Max = masterdata.Max;
            Upgrated = upgradedata;
            ShowValue = showdata;
        }
    }
}