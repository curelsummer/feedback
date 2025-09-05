using System;
using System.Collections.Generic;
using System.Text;

namespace BDF
{
    public class BDFLocalPatientIdentification
    {
        public BDFLocalPatientIdentification()
        {
            //parameterless constructor, required for XML serialization
        }
        public BDFLocalPatientIdentification(char[] patientIdentification)
        {
            parsePatientIdentificationSubFields(patientIdentification);
        }
        public BDFLocalPatientIdentification(string patientCode, string patientSex, DateTime patientBirthDate, string patientName, List<string> patientAdditional)
        {
            setupPatient(patientCode, patientSex, patientBirthDate, patientName, patientAdditional);
        }
        public BDFLocalPatientIdentification(string patientCode, string patientSex, DateTime patientBirthDate, string patientName)
        {
            setupPatient(patientCode, patientSex, patientBirthDate, patientName, new List<string>());
        }
        private void setupPatient(string patientCode, string patientSex, DateTime patientBirthDate, string patientName, List<string> patientAdditional)
        {
            this.PatientCode = patientCode;
            this.PatientSex = patientSex;
            this.PatientBirthDate = patientBirthDate;
            this.PatientName = patientName;
            this.AdditionalPatientIdentification = patientAdditional;
        }
        public string PatientCode { get; set; }
        public string PatientSex { get; set; }
        public DateTime PatientBirthDate { get; set; }
        public string PatientName { get; set; }
        public List<string> AdditionalPatientIdentification { get; set; }
        private StringBuilder _strPatientIdentification = new StringBuilder(String.Empty);

        private void parsePatientIdentificationSubFields(char[] patientIdentification)
        {
            _strPatientIdentification = new StringBuilder(new string(patientIdentification));
            string[] arrayPatientInformation = _strPatientIdentification.ToString().Trim().Split(' ');
            this.AdditionalPatientIdentification = new List<string>();

            if (arrayPatientInformation.Length >= 4)
            {
                this.PatientCode = arrayPatientInformation[0];
                this.PatientSex = arrayPatientInformation[1];
                try
                {
                    this.PatientBirthDate = DateTime.Parse(arrayPatientInformation[2]);
                }
                catch (FormatException ex)
                {
                    System.Diagnostics.Debug.WriteLine("A FormatException occurred on the Patient BirthDate, this is not in BDF+ format\n\n" + ex.StackTrace);
                    this.intializeBDF();
                    return;
                }
                this.PatientName = arrayPatientInformation[3];


                for (int i = 4; i < arrayPatientInformation.Length; i++)
                {
                    AdditionalPatientIdentification.Add(arrayPatientInformation[i]);
                }
            }
            else
            {
                this.intializeBDF();
            }
        }
        private void intializeBDF()
        {
            this.PatientCode = "X";
            this.PatientSex = "X";
            this.PatientBirthDate = DateTime.MinValue;
            this.PatientName = "X";
        }
        public override String ToString()
        {
            _strPatientIdentification = new StringBuilder(string.Empty);
            _strPatientIdentification.Append(PatientCode);
            _strPatientIdentification.Append(" ");
            _strPatientIdentification.Append(PatientSex);
            _strPatientIdentification.Append(" ");
            if (!PatientBirthDate.Equals(DateTime.MinValue))
            {
                _strPatientIdentification.Append(PatientBirthDate.ToString("dd-MMM-yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpper());
            }
            _strPatientIdentification.Append(" ");
            _strPatientIdentification.Append(PatientName);
            if (AdditionalPatientIdentification != null)
                foreach (string info in AdditionalPatientIdentification)
                {
                    _strPatientIdentification.Append(" ");
                    _strPatientIdentification.Append(info);
                }
            _strPatientIdentification = new StringBuilder(_strPatientIdentification.Length > BDFHeader.FixedLength_LocalPatientIdentification ? _strPatientIdentification.ToString().Substring(0, BDFHeader.FixedLength_LocalPatientIdentification) : _strPatientIdentification.ToString().PadRight(BDFHeader.FixedLength_LocalPatientIdentification));
            return _strPatientIdentification.ToString();

        }
    }
}
