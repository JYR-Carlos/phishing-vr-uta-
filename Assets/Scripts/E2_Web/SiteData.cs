using System;
using UnityEngine;

namespace PhishingVR.E2
{
    [Flags]
    public enum PhishingIndicator
    {
        None                = 0,
        BadURL              = 1 << 0,
        NoHTTPS             = 1 << 1,
        TypoInLogo          = 1 << 2,
        ExcessiveFormFields = 1 << 3
    }

    [CreateAssetMenu(menuName = "PhishingVR/Site Data", fileName = "NewSiteData")]
    public class SiteData : ScriptableObject
    {
        public string SiteId;
        public string DisplayName;
        public string DisplayURL;
        public bool HasHTTPS;
        public string LogoText;
        public bool IsPhishing;
        public PhishingIndicator ActiveIndicators;
        public string[] FormFieldLabels;
        [TextArea(3, 6)]
        public string EducationalExplanation;
    }
}
