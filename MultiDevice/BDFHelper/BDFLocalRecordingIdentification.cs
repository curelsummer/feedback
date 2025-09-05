using System;
using System.Collections.Generic;
using System.Text;

namespace BDF
{
    public class BDFLocalRecordingIdentification
    {
        public BDFLocalRecordingIdentification()
        {
            //parameterless constructor, required for XML serialization
        }
        public BDFLocalRecordingIdentification(char[] recordingIdentification)
        {
            parseRecordingIdentificationSubFields(recordingIdentification);
        }

        public BDFLocalRecordingIdentification(string startdate1, string recordcode2, string technician3, string equipment4, List<string> additionalRecordingIdentification)
        {
            this.AdditionalRecordingIdentification = new List<string>();
            this.RecordingStartDate = DateTime.Parse(startdate1);
            this.RecordingCode = recordcode2;
            this.RecordingTechnician = technician3;
            this.RecordingEquipment = equipment4;
            for (int i = 0; i < additionalRecordingIdentification.Count; i++)
            {
                AdditionalRecordingIdentification.Add(additionalRecordingIdentification[i]);
            }
        }


        public BDFLocalRecordingIdentification(string startdate1, string recordcode2, string technician3, string equipment4)
        {
            this.AdditionalRecordingIdentification = new List<string>();
            this.RecordingStartDate = DateTime.Parse(startdate1);
            this.RecordingCode = recordcode2;
            this.RecordingTechnician = technician3;
            this.RecordingEquipment = equipment4;
        }

        public DateTime RecordingStartDate { get; set; }
        public string RecordingCode { get; set; }
        public string RecordingTechnician { get; set; }
        public string RecordingEquipment { get; set; }
        public List<string> AdditionalRecordingIdentification { get; set; }
        private StringBuilder _strRecordingIdentification = new StringBuilder(string.Empty);

        private void parseRecordingIdentificationSubFields(char[] recordingIdentification)
        {
            _strRecordingIdentification = new StringBuilder(new string(recordingIdentification));
            this.AdditionalRecordingIdentification = new List<string>();

            string[] arrayRecordingInformation = _strRecordingIdentification.ToString().Trim().Split(' ');
            if (arrayRecordingInformation.Length >= 5)
            {
                if (!arrayRecordingInformation[0].ToLower().Equals("startdate"))
                {
                    this.intializeBDF();
                    throw new ArgumentException("Recording Identification must start with the text 'Startdate'");
                }
                try
                {
                    this.RecordingStartDate = DateTime.Parse(arrayRecordingInformation[1]);
                }
                catch (FormatException ex)
                {
                    System.Diagnostics.Debug.WriteLine("A FormatException occurred on the Recording Start Date, this is not in BDF+ format\n\n" + ex.StackTrace);
                    this.intializeBDF();
                    return;
                }
                this.RecordingCode = arrayRecordingInformation[2];
                this.RecordingTechnician = arrayRecordingInformation[3];
                this.RecordingEquipment = arrayRecordingInformation[4];
                for (int i = 5; i < arrayRecordingInformation.Length; i++)
                {
                    AdditionalRecordingIdentification.Add(arrayRecordingInformation[i]);
                }
            }
            else
            {
                this.intializeBDF();
            }
        }
        private void intializeBDF()
        {
            this.RecordingStartDate = DateTime.MinValue;
            this.RecordingCode = "X";
            this.RecordingTechnician = "X";
            this.RecordingEquipment = "X";
        }
        public override String ToString()
        {

            _strRecordingIdentification = new StringBuilder(string.Empty);
            _strRecordingIdentification.Append("Startdate");
            _strRecordingIdentification.Append(" ");
            _strRecordingIdentification.Append(this.RecordingStartDate.ToString("dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpper());
            _strRecordingIdentification.Append(" ");
            _strRecordingIdentification.Append(this.RecordingCode);
            _strRecordingIdentification.Append(" ");
            _strRecordingIdentification.Append(this.RecordingTechnician);
            _strRecordingIdentification.Append(" ");
            _strRecordingIdentification.Append(this.RecordingEquipment);
            if (AdditionalRecordingIdentification != null)
                foreach (string info in AdditionalRecordingIdentification)
                {
                    _strRecordingIdentification.Append(" ");
                    _strRecordingIdentification.Append(info);
                }
            _strRecordingIdentification = new StringBuilder(_strRecordingIdentification.Length > BDFHeader.FixedLength_LocalRecordingIdentifiaction ? _strRecordingIdentification.ToString().Substring(0, BDFHeader.FixedLength_LocalRecordingIdentifiaction) : _strRecordingIdentification.ToString().PadRight(BDFHeader.FixedLength_LocalRecordingIdentifiaction));
            return _strRecordingIdentification.ToString();
        }
    }
}
