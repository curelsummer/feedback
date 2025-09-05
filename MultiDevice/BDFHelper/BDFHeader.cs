using System;
using System.Collections.Generic;
using System.Text;

namespace BDF
{
    public class BDFHeader
    {
        public static string BDFContinuous = "BDF+C";
        public static string BDFDiscontinuous = "BDF+D";
         
        private bool _isBDFPlus = false;

        public bool Continuous = true;

        public bool IsBDFPlus
        {
            get
            {
                return _isBDFPlus;
            }
            set
            {
                _isBDFPlus = value;
            }
        }

        public BDFHeader()
        {
            initializeBDFHeader();

        }
        public BDFHeader(bool isBDFPlus)
        {
            this._isBDFPlus = isBDFPlus;
            initializeBDFHeader();
        }
        public BDFHeader(char[] header)
        {
            if (header.Length != 256)
            {
                throw new ArgumentException("Header must be 256 characters");
            }
            parseHeader(header);
        }
        private void initializeBDFHeader()
        {
            this.Signals = new List<BDFSignal>(); 

            this._PatientInformation = new BDFLocalPatientIdentification(getFixedLengthString(string.Empty, BDFHeader.FixedLength_LocalPatientIdentification).ToCharArray());
            this._RecordingInformation = new BDFLocalRecordingIdentification(getFixedLengthString(string.Empty, BDFHeader.FixedLength_LocalRecordingIdentifiaction).ToCharArray());
            this.StartDateBDF = DateTime.MinValue.ToString("dd.MM.yy", System.Globalization.CultureInfo.InvariantCulture);
            this.StartTimeBDF = DateTime.MinValue.ToString("hh.mm.ss", System.Globalization.CultureInfo.InvariantCulture);
            this.NumberOfBytes = 0;
            this.NumberOfDataRecords = 0;
            this.DurationOfDataRecordInSeconds = 0;
            this.NumberOfSignalsInDataRecord = this.Signals.Count;
            this.Reserved = string.Empty;
        } 
        public static int FixedLength_LocalPatientIdentification = 80;
        private BDFLocalPatientIdentification _PatientInformation;
        public BDFLocalPatientIdentification PatientIdentification
        {
            get
            {
                return _PatientInformation;
            }
            set
            {
                if (value.ToString().Length != FixedLength_LocalPatientIdentification)
                {
                    throw new FormatException("Patient Information must be " + FixedLength_LocalPatientIdentification + " characters fixed length");
                }
                _PatientInformation = value;
            }
        }

        public static int FixedLength_LocalRecordingIdentifiaction = 80;
        private BDFLocalRecordingIdentification _RecordingInformation;
        public BDFLocalRecordingIdentification RecordingIdentification
        {
            get
            {
                return _RecordingInformation;
            }
            set
            {
                if (value.ToString().Length != BDFHeader.FixedLength_LocalRecordingIdentifiaction)
                {
                    throw new FormatException("Recording Information must be " + BDFHeader.FixedLength_LocalRecordingIdentifiaction + " characters fixed length");
                }
                _RecordingInformation = value;
            }
        }

        public static int FixedLength_StartDateBDF = 8;
        public string StartDateBDF { get; private set; }

        public static int FixedLength_StartTimeBDF = 8;
        public string StartTimeBDF { get; private set; }

        private DateTime _StartDateTime;
        public DateTime StartDateTime
        {
            get { return _StartDateTime; }
            set
            {
                this.StartDateBDF = value.ToString("dd.MM.yy", System.Globalization.CultureInfo.InvariantCulture).ToUpper();
                this.StartTimeBDF = value.ToString("HH.mm.ss", System.Globalization.CultureInfo.InvariantCulture);
                _StartDateTime = value;
            }
        }

        public static int FixedLength_NumberOfBytes = 8;
        private string _NumberOfBytesFixedLengthString = "0";
        private int _NumberOfBytes = 0;
        public int NumberOfBytes
        {
            get
            {
                return _NumberOfBytes;
            }
            set
            {
                _NumberOfBytes = value;
                _NumberOfBytesFixedLengthString = getFixedLengthString(Convert.ToString(value), FixedLength_NumberOfBytes);
            }
        }

        public static int FixedLength_NumberOfDataRecords = 8;
        private string _NumberOfDataRecordsFixedLengthString = "0";
        private int _NumberOfDataRecords = 0;
        public int NumberOfDataRecords
        {
            get
            {
                return _NumberOfDataRecords;
            }
            set
            {
                _NumberOfDataRecords = value;
                _NumberOfDataRecordsFixedLengthString = getFixedLengthString(Convert.ToString(value), FixedLength_NumberOfDataRecords);
            }
        }

        public static int FixedLength_DuraitonOfDataRecordInSeconds = 8;
        private string _DurationOfDataRecordInSecondsFixedLengthString = "0";
        private double _DurationOfDataRecordInSeconds = 0;
        public double DurationOfDataRecordInSeconds
        {
            get
            {
                return _DurationOfDataRecordInSeconds;
            }
            set
            {
                _DurationOfDataRecordInSeconds = value;
                _DurationOfDataRecordInSecondsFixedLengthString = getFixedLengthString(Convert.ToString(value), FixedLength_DuraitonOfDataRecordInSeconds);
            }
        }

        public static int FixedLength_NumberOfSignalsInDataRecord = 4;
        private string _NumberOfSignalsInDataRecordFixedLengthString = "0";
        private int _NumberOfSignalsInDataRecord = 0;
        public int NumberOfSignalsInDataRecord
        {
            get
            {
                return _NumberOfSignalsInDataRecord;
            }
            set
            {
                _NumberOfSignalsInDataRecord = value;
                _NumberOfSignalsInDataRecordFixedLengthString = getFixedLengthString(Convert.ToString(value), FixedLength_NumberOfSignalsInDataRecord);
            }
        }

        public static int FixedLength_Reserved = 44;
        private string _Reserved;
        public string Reserved
        {
            get
            {
                return _Reserved;
            }
            set
            {
                _Reserved = getFixedLengthString(value, FixedLength_Reserved);
            }
        }


        public List<BDFSignal> Signals { get; set; }
        private StringBuilder _strHeader = new StringBuilder(string.Empty);

        private void parseHeader(char[] header)
        {
            /**
             * replace nulls with space.
             */
            int i = 0;
            foreach (char c in header)
            {
                if (header[i] == (char)0)
                {
                    header[i] = (char)32;
                }
                i++;
            }

            _strHeader.Append(header);

            int fileIndex = 0;
             
            fileIndex += 8;

            char[] localPatientIdentification = getFixedLengthCharArrayFromHeader(header, fileIndex, BDFHeader.FixedLength_LocalPatientIdentification);
            fileIndex += BDFHeader.FixedLength_LocalPatientIdentification;

            char[] localRecordingIdentification = getFixedLengthCharArrayFromHeader(header, fileIndex, BDFHeader.FixedLength_LocalRecordingIdentifiaction);
            fileIndex += BDFHeader.FixedLength_LocalRecordingIdentifiaction;

            char[] startDate = getFixedLengthCharArrayFromHeader(header, fileIndex, BDFHeader.FixedLength_StartDateBDF);
            this.StartDateBDF = new string(startDate);
            fileIndex += BDFHeader.FixedLength_StartDateBDF;

            char[] startTime = getFixedLengthCharArrayFromHeader(header, fileIndex, BDFHeader.FixedLength_StartTimeBDF);
            this.StartTimeBDF = new string(startTime);
            fileIndex += BDFHeader.FixedLength_StartTimeBDF;

            char[] numberOfBytesInHeaderRow = getFixedLengthCharArrayFromHeader(header, fileIndex, BDFHeader.FixedLength_NumberOfBytes);
            this.NumberOfBytes = int.Parse(new string(numberOfBytesInHeaderRow).Trim());
            fileIndex += BDFHeader.FixedLength_NumberOfBytes;

            char[] reserved = getFixedLengthCharArrayFromHeader(header, fileIndex, BDFHeader.FixedLength_Reserved);
            if (new string(reserved).StartsWith(BDFHeader.BDFContinuous) || new string(reserved).StartsWith(BDFHeader.BDFDiscontinuous))
            {
                this._isBDFPlus = true;
            }
            this.Reserved = new string(reserved);
            fileIndex += BDFHeader.FixedLength_Reserved;

            char[] numberOfDataRecords = getFixedLengthCharArrayFromHeader(header, fileIndex, BDFHeader.FixedLength_NumberOfDataRecords);
            this.NumberOfDataRecords = (int.Parse(new string(numberOfDataRecords).Trim()));
            fileIndex += BDFHeader.FixedLength_NumberOfDataRecords;

            char[] durationOfDataRecord = getFixedLengthCharArrayFromHeader(header, fileIndex, BDFHeader.FixedLength_DuraitonOfDataRecordInSeconds);
            this.DurationOfDataRecordInSeconds = double.Parse(new string(durationOfDataRecord).Trim());
            fileIndex += BDFHeader.FixedLength_DuraitonOfDataRecordInSeconds;

            char[] numberOfSignals = getFixedLengthCharArrayFromHeader(header, fileIndex, BDFHeader.FixedLength_NumberOfSignalsInDataRecord);
            this.NumberOfSignalsInDataRecord = int.Parse(new string(numberOfSignals).Trim());
            if (this.NumberOfSignalsInDataRecord < 1 || this.NumberOfSignalsInDataRecord > 256)
            {
                throw new ArgumentException("BDF File has " + this.NumberOfSignalsInDataRecord + " Signals; Number of Signals must be >1 and <=256");
            }
            fileIndex += BDFHeader.FixedLength_NumberOfSignalsInDataRecord;

            this.PatientIdentification = new BDFLocalPatientIdentification(localPatientIdentification);
            this.RecordingIdentification = new BDFLocalRecordingIdentification(localRecordingIdentification);

            this.StartDateTime = DateTime.ParseExact(this.StartDateBDF + " " + this.StartTimeBDF, "dd.MM.yy HH.mm.ss", System.Globalization.CultureInfo.InvariantCulture);
            if (this.IsBDFPlus)
            {
                if (!this.StartDateTime.Date.Equals(this.RecordingIdentification.RecordingStartDate))
                {
                    throw new ArgumentException("Header StartDateTime does not equal Header.RecordingIdentification StartDate!");
                }
                else
                {
                    this.RecordingIdentification.RecordingStartDate = this.StartDateTime;
                }
            }


        }
        public void parseSignals(char[] signals)
        {
            this._strHeader.Append(signals);

            this.Signals = new List<BDFSignal>();

            /**
             * replace nulls with space.
             */
            int h = 0;
            foreach (char c in signals)
            {
                if (signals[h] == (char)0)
                {
                    signals[h] = (char)32;
                }
                h++;
            }

            for (int i = 0; i < this.NumberOfSignalsInDataRecord; i++)
            {
                BDFSignal BDF_signal = new BDFSignal();

                int charIndex = 0;

                char[] label = getFixedLengthCharArrayFromHeader(signals, (i * 16) + (this.NumberOfSignalsInDataRecord * charIndex), 16);
                BDF_signal.Label = new string(label);
                charIndex += 16;

                BDF_signal.IndexNumber = (i + 1);

                char[] transducer_type = getFixedLengthCharArrayFromHeader(signals, (i * 80) + (this.NumberOfSignalsInDataRecord * charIndex), 80);
                BDF_signal.TransducerType = new string(transducer_type);
                charIndex += 80;

                char[] physical_dimension = getFixedLengthCharArrayFromHeader(signals, (i * 8) + (this.NumberOfSignalsInDataRecord * charIndex), 8);
                BDF_signal.PhysicalDimension = new string(physical_dimension);
                charIndex += 8;

                char[] physical_min = getFixedLengthCharArrayFromHeader(signals, (i * 8) + (this.NumberOfSignalsInDataRecord * charIndex), 8);
                BDF_signal.PhysicalMinimum = double.Parse(new string(physical_min).Trim());
                charIndex += 8;

                char[] physical_max = getFixedLengthCharArrayFromHeader(signals, (i * 8) + (this.NumberOfSignalsInDataRecord * charIndex), 8);
                BDF_signal.PhysicalMaximum = double.Parse(new string(physical_max).Trim());
                charIndex += 8;

                char[] digital_min = getFixedLengthCharArrayFromHeader(signals, (i * 8) + (this.NumberOfSignalsInDataRecord * charIndex), 8);
                BDF_signal.DigitalMinimum = double.Parse(new string(digital_min).Trim());
                charIndex += 8;

                char[] digital_max = getFixedLengthCharArrayFromHeader(signals, (i * 8) + (this.NumberOfSignalsInDataRecord * charIndex), 8);
                BDF_signal.DigitalMaximum = double.Parse(new string(digital_max).Trim());
                charIndex += 8;

                char[] prefiltering = getFixedLengthCharArrayFromHeader(signals, (i * 80) + (this.NumberOfSignalsInDataRecord * charIndex), 80);
                BDF_signal.Prefiltering = new string(prefiltering);
                charIndex += 80;

                char[] samples_each_datarecord = getFixedLengthCharArrayFromHeader(signals, (i * 8) + (this.NumberOfSignalsInDataRecord * charIndex), 8);
                BDF_signal.NumberOfSamplesPerDataRecord = int.Parse(new string(samples_each_datarecord).Trim());
                charIndex += 8;

                this.Signals.Add(BDF_signal);

            }

        }
        private string getFixedLengthString(string input, int length)
        {
            return (input ?? "").Length > length ? (input ?? "").Substring(0, length) : (input ?? "").PadRight(length);
        }
        private char[] getFixedLengthCharArrayFromHeader(char[] header, int startPoint, int length)
        {
            char[] ch = new char[length];
            Array.Copy(header, startPoint, ch, 0, length);
            return ch;

        }
        public override string ToString()
        {
            StringBuilder _strHeaderBuilder = new StringBuilder(string.Empty);

            char[] tmp_bytes = new char[8];
            tmp_bytes[0] = (char)0xff;
            tmp_bytes[1] = 'B';
            tmp_bytes[2] = 'I';
            tmp_bytes[3] = 'O';
            tmp_bytes[4] = 'S';
            tmp_bytes[5] = 'E';
            tmp_bytes[6] = 'M';
            tmp_bytes[7] = 'I';

            _strHeaderBuilder.Append(new string(tmp_bytes));
            _strHeaderBuilder.Append(getFixedLengthString(this.PatientIdentification.ToString(), 80));
            _strHeaderBuilder.Append(getFixedLengthString(this.RecordingIdentification.ToString(), 80));
            _strHeaderBuilder.Append(getFixedLengthString(this.StartDateBDF, 8));
            _strHeaderBuilder.Append(getFixedLengthString(this.StartTimeBDF, 8));
            _strHeaderBuilder.Append(getFixedLengthString(Convert.ToString(this.NumberOfBytes), BDFHeader.FixedLength_NumberOfBytes));

            if (this.IsBDFPlus)
            {
                if (this.Continuous)
                    Reserved = BDFHeader.BDFContinuous;
                else
                    Reserved = BDFHeader.BDFDiscontinuous;
            }

            _strHeaderBuilder.Append(this.Reserved);
            _strHeaderBuilder.Append(getFixedLengthString(Convert.ToString(this.NumberOfDataRecords), BDFHeader.FixedLength_NumberOfDataRecords));
            _strHeaderBuilder.Append(getFixedLengthString(Convert.ToString(this.DurationOfDataRecordInSeconds), BDFHeader.FixedLength_DuraitonOfDataRecordInSeconds));
            _strHeaderBuilder.Append(getFixedLengthString(Convert.ToString(this.NumberOfSignalsInDataRecord), BDFHeader.FixedLength_NumberOfSignalsInDataRecord));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString(s.Label, 16));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString(s.TransducerType, 80));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString(s.PhysicalDimension, 8));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString(Convert.ToString(s.PhysicalMinimum), 8));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString(Convert.ToString(s.PhysicalMaximum), 8));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString(Convert.ToString(s.DigitalMinimum), 8));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString(Convert.ToString(s.DigitalMaximum), 8));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString(s.Prefiltering, 80));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString(Convert.ToString(s.NumberOfSamplesPerDataRecord), 8));
            foreach (BDFSignal s in this.Signals)
                _strHeaderBuilder.Append(getFixedLengthString("", 32));

            if (_strHeaderBuilder.ToString().ToCharArray().Length != (256 + (this.Signals.Count * 256)))
            {
                throw new InvalidOperationException("Header Length must be equal to (256 characters + (number of signals) * 256 ).  Header length=" + _strHeaderBuilder.ToString().ToCharArray().Length + "  Header=" + _strHeaderBuilder.ToString());
            }
            _strHeader = _strHeaderBuilder;
            return _strHeaderBuilder.ToString();
        }

    }
}
