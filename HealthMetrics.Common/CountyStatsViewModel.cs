﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.Common
{
    using ProtoBuf;
    using System.Runtime.Serialization;

    [DataContract]
    [ProtoContract]
    public struct CountyStatsViewModel
    {
        public CountyStatsViewModel(int doctorCount, int patientCount, long healthReportCount, HealthIndex averageHealthIndex)
        {
            this.AverageHealthIndex = averageHealthIndex;
            this.DoctorCount = doctorCount;
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
        }

        [DataMember]
        [ProtoMember(1)]
        public int DoctorCount { get; private set; }

        [DataMember]
        [ProtoMember(2)]
        public int PatientCount { get; private set; }

        [DataMember]
        [ProtoMember(3)]
        public long HealthReportCount { get; private set; }

        [DataMember]
        [ProtoMember(4)]
        public HealthIndex AverageHealthIndex { get; private set; }
    }
}