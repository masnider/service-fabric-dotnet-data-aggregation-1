﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace HealthMetrics.NationalService.Models
{
    using System.Runtime.Serialization;
    using HealthMetrics.Common;
    using ProtoBuf;

    [ProtoContract]
    internal struct NationalCountyStats
    {
        public NationalCountyStats(int doctorCount, int patientCount, long healthReportCount, HealthIndex averageHealthIndex)
        {
            this.AverageHealthIndex = averageHealthIndex;
            this.DoctorCount = doctorCount;
            this.PatientCount = patientCount;
            this.HealthReportCount = healthReportCount;
        }

        [ProtoMember(1)]
        public int DoctorCount { get; private set; }

        [ProtoMember(2)]
        public int PatientCount { get; private set; }

        [ProtoMember(3)]
        public long HealthReportCount { get; private set; }

        [ProtoMember(4)]
        public HealthIndex AverageHealthIndex { get; private set; }
    }
}