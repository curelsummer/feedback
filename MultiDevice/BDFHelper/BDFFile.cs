using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using System.Linq;
using System.Threading;

namespace BDF
{
    public class BDFFile
    {
        public BDFFile()
        {
            //initialize BDFHeader as part of constructor
            _header = new BDFHeader();
            //initialize BDFDataRecord List as part of constructor
            _dataRecords = new List<BDFDataRecord>();
            NumOfRecordsRead = 0;
        }


        public int NumOfRecordsRead = 0;
        string theFilePath;
        public string TheFilePath { get { return theFilePath; } set { theFilePath = value; hasSetFilePath = true; } }
        public BDFFile(string tmp)
        {
            //initialize BDFHeader as part of constructor
            _header = new BDFHeader();
            //initialize BDFDataRecord List as part of constructor
            _dataRecords = new List<BDFDataRecord>();
            theFilePath = tmp;
            hasSetFilePath = true;
            NumOfRecordsRead = 0;
        }

        private BDFHeader _header;
        public BDFHeader Header
        {
            get
            {
                return _header;
            }
        }

        private List<BDFDataRecord> _dataRecords;
        public List<BDFDataRecord> DataRecords
        {
            get
            {
                return _dataRecords;
            }
        }


        public void readFile()
        {
            if (!File.Exists(theFilePath))
                throw new FileNotFoundException("the file doesn't exists");
            else
            {
                FileStream file = new FileStream(theFilePath, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(file);
                readStream(sr);
                file.Close();
                sr.Close();
            }
        }

        public bool UseMemCache = true;
        public bool hasSetFilePath = false;
        public bool hasSavedHeader = false;

        public void addDataRecord(BDFDataRecord dataRecord)
        {
            if (UseMemCache)
                throw new ArgumentException("you used memory cache");
            if (!hasSetFilePath)
                throw new ArgumentException("FilePath not set");

            if (!hasSavedHeader)
            {

                FileStream newtmpFile = new FileStream(theFilePath, FileMode.CreateNew, FileAccess.Write);
               
                this.Header.NumberOfDataRecords = -1;

                this.Header.NumberOfBytes = 256 + this.Header.NumberOfSignalsInDataRecord * 256;
                char[] headerCharArray = this.Header.ToString().ToCharArray();

                var xxx = headerCharArray.Select(c => (byte)c).ToArray();
                newtmpFile.Seek(0, SeekOrigin.Begin);
                newtmpFile.Write(xxx, 0, xxx.Length);
               
                newtmpFile.Close();
                hasSavedHeader = true;
                this.Header.NumberOfDataRecords = 0;
            }
            List<byte> byteList = new List<byte>();
            byte[] byteArraySample = new byte[3];
            foreach (BDFSignal signal in this.Header.Signals)
            {
                if (signal.Label.Trim() != "BDF Annotations")
                {
                    foreach (double sample in dataRecord.Signals[signal.IndexNumberWithLabel])
                    {
                        var csample = sample * 256;
                        var xxx = (csample / signal.AmplifierGain) - signal.Offset;
                        if (xxx > Int32.MaxValue)
                            xxx = Int32.MaxValue;
                        if (xxx <  Int32.MinValue)
                            xxx = Int32.MinValue;

                        var tmp_bytes = BitConverter.GetBytes(Convert.ToInt32(xxx));
                        byteArraySample[0] = tmp_bytes[1];
                        byteArraySample[1] = tmp_bytes[2];
                        byteArraySample[2] = tmp_bytes[3];

                        byteList.Add(byteArraySample[0]);
                        byteList.Add(byteArraySample[1]);
                        byteList.Add(byteArraySample[2]);
                    }
                }
                else
                {
                    List<byte> tmp = dataRecord.getAnnotationListByte();
                    if (tmp.Count < this.Header.Signals[this.Header.Signals.Count - 1].NumberOfSamplesPerDataRecord * 3)
                        while (tmp.Count < this.Header.Signals[this.Header.Signals.Count - 1].NumberOfSamplesPerDataRecord * 3)
                        {
                            tmp.Add(0x00);
                        }
                    else
                    {
                        throw new ArgumentException("annotation convert to byte:to much words");
                    }
                    byteList.AddRange(tmp);
                }
            }
            //xxx
            //newFile.Seek((256 + this.Header.NumberOfSignalsInDataRecord * 256 + byteList.Count * this.Header.NumberOfDataRecords), SeekOrigin.Begin);

            //FileStream newFile = new FileStream(theFilePath, FileMode.Append, FileAccess.Write);
            //newFile.Seek(0, SeekOrigin.End);
            //BinaryWriter bw = new BinaryWriter(newFile);

            //byte[] byteListArray = byteList.ToArray();
            //bw.Write(byteListArray, 0, byteListArray.Length);
            //bw.Flush();
            //bw.Close();
            //newFile.Close();

            ThreadPool.QueueUserWorkItem(xxx => {
                using (FileStream newFile = new FileStream(theFilePath, FileMode.Append, FileAccess.Write))
                using (BinaryWriter bw = new BinaryWriter(newFile))
                {
                    newFile.Seek(0, SeekOrigin.End);

                    byte[] byteListArray = byteList.ToArray();
                    bw.Write(byteListArray, 0, byteListArray.Length);
                    bw.Flush();
                }
            });

            this.Header.NumberOfDataRecords++;
        }
        public void SyncHeader()
        {
            if (UseMemCache)
                throw new ArgumentException("Error,you used memory cache");
            if (!hasSetFilePath)
                throw new ArgumentException("FilePath not set");
            FileStream newFile = new FileStream(theFilePath, FileMode.Open, FileAccess.Write);
            //newFile.Seek(236, SeekOrigin.Begin);
            //char[] numofrecords = getFixedLengthString(Convert.ToString(this.Header.NumberOfDataRecords), 8).ToCharArray();
            newFile.Seek(0, SeekOrigin.Begin);
            char[] headerCharArray = this.Header.ToString().ToCharArray();
            var xxx = headerCharArray.Select(c => (byte)c).ToArray();
            xxx[0] = 0xff;
            newFile.Write(xxx, 0, xxx.Length);
            //ssswww.Write(numofrecords, 0, numofrecords.Length);
            //StreamWriter ssswww = new StreamWriter(newFile);
            //ssswww.Write(headerCharArray, 0, headerCharArray.Length);
            //ssswww.Close();

            newFile.Close();

            this.Header.NumberOfDataRecords = 0;
            hasSavedHeader = false;
        }
        private string getFixedLengthString(string input, int length)
        {
            return (input ?? "").Length > length ? (input ?? "").Substring(0, length) : (input ?? "").PadRight(length);
        }
        private bool gotHeader = false;

        public void readHeader()
        {
            if (!File.Exists(theFilePath))
                throw new FileNotFoundException("the file doesn't exists");
            else
            {
                FileStream file = new FileStream(theFilePath, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(file);
                parseHeaderStream(sr);
                gotHeader = true;
                file.Close();
                sr.Close();
            }
        }

        public void readRadomDataRecords(int from, int to)
        {
            if (!File.Exists(theFilePath))
                throw new FileNotFoundException("the file doesn't exists");
            else
            {
                FileStream file = new FileStream(theFilePath, FileMode.Open, FileAccess.Read);
                StreamReader sr = new StreamReader(file);
                if (!gotHeader)
                {
                    parseHeaderStream(sr);
                    gotHeader = true;
                }

                if (from < 0)
                    from = 0;
                if (to >= this.Header.NumberOfDataRecords)
                    to = this.Header.NumberOfDataRecords - 1;
                //clear _dataRecords everytime before reading readRadom records
                _dataRecords.Clear();
                NumOfRecordsRead = 0;
                int dataRecordSize = 0;
                foreach (BDFSignal signal in this.Header.Signals)
                {
                    signal.SamplePeriodWithinDataRecord = this.Header.DurationOfDataRecordInSeconds / signal.NumberOfSamplesPerDataRecord;
                    dataRecordSize += signal.NumberOfSamplesPerDataRecord;
                }

                //set the seek position in the file stream to the beginning of the data records.
                sr.BaseStream.Seek((256 + this.Header.NumberOfSignalsInDataRecord * 256 + from * dataRecordSize), SeekOrigin.Begin);

                byte[] dataRecordBytes = new byte[dataRecordSize * 3];


                while (sr.BaseStream.Read(dataRecordBytes, 0, dataRecordSize * 3) > 0)
                {


                    BDFDataRecord dataRecord;
                    int samplesWritten = 0;
                    if (!this.Header.IsBDFPlus)
                    {
                        dataRecord = new BDFDataRecord(Header.StartDateTime);
                        foreach (BDFSignal signal in this.Header.Signals)
                        {
                            List<double> samples = new List<double>();
                            for (int l = 0; l < signal.NumberOfSamplesPerDataRecord; l++)
                            {
                                byte[] tmp_bytes = new byte[4];
                                tmp_bytes[0] = dataRecordBytes[samplesWritten * 3 + 0];
                                tmp_bytes[1] = dataRecordBytes[samplesWritten * 3 + 1];
                                tmp_bytes[2] = dataRecordBytes[samplesWritten * 3 + 2];
                                tmp_bytes[3] = 0x00;
                                double value = (double)(((getValueSigned(tmp_bytes[2], tmp_bytes[1], tmp_bytes[0]) + (int)signal.Offset)) * signal.AmplifierGain);
                                samples.Add(value);
                                samplesWritten++;
                            }
                            dataRecord.Signals.Add(signal.IndexNumberWithLabel, samples);
                        }
                    }
                    else
                    {
                        dataRecord = new BDFDataRecord(Header.StartDateTime);
                        for (int i = 0; i < this.Header.Signals.Count - 1; i++)
                        {
                            BDFSignal signal = this.Header.Signals[i];
                            List<double> samples = new List<double>();
                            for (int l = 0; l < signal.NumberOfSamplesPerDataRecord; l++)
                            {
                                byte[] tmp_bytes = new byte[4];
                                tmp_bytes[0] = dataRecordBytes[samplesWritten * 3 + 0];
                                tmp_bytes[1] = dataRecordBytes[samplesWritten * 3 + 1];
                                tmp_bytes[2] = dataRecordBytes[samplesWritten * 3 + 2];
                                tmp_bytes[3] = 0x00;
                                double value = (double)(((getValueSigned(tmp_bytes[2], tmp_bytes[1], tmp_bytes[0]) + (int)signal.Offset)) * signal.AmplifierGain);
                                samples.Add(value);
                                samplesWritten++;
                            }
                            dataRecord.Signals.Add(signal.IndexNumberWithLabel, samples);
                        }
                        byte[] tmpp = new byte[this.Header.Signals[this.Header.Signals.Count - 1].NumberOfSamplesPerDataRecord * 2];
                        int index = 0;
                        while (index < tmpp.Length)
                        {
                            tmpp[index] = dataRecordBytes[3 * samplesWritten + index];
                            index++;
                        }
                        dataRecord.parseAnnotations(tmpp);
                    }
                    _dataRecords.Add(dataRecord);

                    NumOfRecordsRead++;
                    if (NumOfRecordsRead >= to - from + 1)
                        break;

                }
                sr.Close();
                file.Close();
            }
        }
        public int getValueSigned(byte a, byte b, byte c)
        {
            int low = 0, low2 = 0, high = 0;
            //low = b & 0x01 + b >> 1 & 0x01 * 2 + b >> 2 & 0x01 * 4 + b >> 3 & 0x01 * 8 + b >> 4 & 0x01 * 16
            int tmp = 1;
            for (int i = 0; i < 8; i++)
            {
                low = low + (c >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            tmp = 1;
            for (int i = 0; i < 8; i++)
            {
                low2 = low2 + (b >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            low2 = low2 * 256;
            tmp = 1;
            for (int i = 0; i < 7; i++)
            {
                high = high + (a >> i & 0x01) * tmp;
                tmp = tmp * 2;
            }
            high = high * 256 * 256;


            if ((a >> 7 & 0x01) > 0)
            {
                return -(8388608 - high - low-low2);

            }
            else
            {
                return high + low+low2;
            }
        }
        public void readStream(StreamReader sr)
        {
            parseHeaderStream(sr);
            gotHeader = true;
            parseDataRecordStream(sr);

        }

        public byte[] getBDFFileBytes()
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] byteArray = encoding.GetBytes(this.Header.ToString().ToCharArray());
            byteArray[0] = 0xff;
            List<byte> byteList = new List<byte>(byteArray);
            byteList.AddRange(getCompressedDataRecordsBytes());
            return byteList.ToArray();
        }

        public List<byte> getCompressedDataRecordsBytes()
        {
            List<byte> byteList = new List<byte>();
            byte[] byteArraySample = new byte[3];
            foreach (BDFDataRecord dataRecord in this.DataRecords)
            {
                foreach (BDFSignal signal in this.Header.Signals)
                {
                    if (signal.Label.Trim() != "BDF Annotations")
                    {
                        foreach (double sample in dataRecord.Signals[signal.IndexNumberWithLabel])
                        { 
                            var csample = sample * 256;
                            var xxx = (csample / signal.AmplifierGain) - signal.Offset;
                            if (xxx > Int32.MaxValue)
                                xxx = Int32.MaxValue;
                            if (xxx < Int32.MinValue)
                                xxx = Int32.MinValue;

                            var tmp_bytes = BitConverter.GetBytes(Convert.ToInt32(xxx));
                            byteArraySample[0] = tmp_bytes[1];
                            byteArraySample[1] = tmp_bytes[2];
                            byteArraySample[2] = tmp_bytes[3];
                            byteList.Add(byteArraySample[0]);
                            byteList.Add(byteArraySample[1]);
                            byteList.Add(byteArraySample[2]);
                        }
                    }
                    else
                    {
                        List<byte> tmp = dataRecord.getAnnotationListByte();
                        if (tmp.Count < this.Header.Signals[this.Header.Signals.Count - 1].NumberOfSamplesPerDataRecord *3)
                            while (tmp.Count < this.Header.Signals[this.Header.Signals.Count - 1].NumberOfSamplesPerDataRecord * 3)
                            {
                                tmp.Add(0x00);
                            }
                        else
                        {
                            throw new ArgumentException("annotation convert to byte:to much words");
                        }

                        byteList.AddRange(tmp);
                    }
                }

                //if (this.Header.IsBDFPlus)
                //{

                //}
            }
            return byteList;
        }



        public void saveFile(string file_path)
        {
            if (File.Exists(file_path))
            {
                File.Delete(file_path);
            }

            FileStream newFile = new FileStream(file_path, FileMode.CreateNew, FileAccess.Write);
             
            this.Header.NumberOfDataRecords = this.DataRecords.Count;

            this.Header.NumberOfBytes = 256 + this.Header.NumberOfSignalsInDataRecord * 256;
            
            newFile.Seek(0, SeekOrigin.Begin);
            char[] headerCharArray = this.Header.ToString().ToCharArray();
            var xxx = headerCharArray.Select(c => (byte)c).ToArray();
            xxx[0] = 0xff;
            newFile.Write(xxx, 0, xxx.Length);


            newFile.Seek((256 + this.Header.NumberOfSignalsInDataRecord * 256), SeekOrigin.Begin);
            BinaryWriter bw = new BinaryWriter(newFile);

            byte[] byteList = getCompressedDataRecordsBytes().ToArray();

            bw.Write(byteList, 0, byteList.Length);
            bw.Flush();
            bw.Close();
            newFile.Close();
        }

        private void parseHeaderStream(StreamReader sr)
        {
            //parse the header to get the number of Signals (size of the Singal Header)
            char[] header = new char[256];
            sr.ReadBlock(header, 0, 256);
            this._header = new BDFHeader(header);

            //parse the signals within the header
            char[] signals = new char[this.Header.NumberOfSignalsInDataRecord * 256];
            sr.ReadBlock(signals, 0, this.Header.NumberOfSignalsInDataRecord * 256);
            this.Header.parseSignals(signals);

        }

        private void parseDataRecordStream(StreamReader sr)
        {

            //set the seek position in the file stream to the beginning of the data records.
            sr.BaseStream.Seek((256 + this.Header.NumberOfSignalsInDataRecord * 256), SeekOrigin.Begin);

            int dataRecordSize = 0;
            foreach (BDFSignal signal in this.Header.Signals)
            {
                signal.SamplePeriodWithinDataRecord = this.Header.DurationOfDataRecordInSeconds / signal.NumberOfSamplesPerDataRecord;
                dataRecordSize += signal.NumberOfSamplesPerDataRecord;
            }

            byte[] dataRecordBytes = new byte[dataRecordSize * 3];

            while (sr.BaseStream.Read(dataRecordBytes, 0, dataRecordSize * 3) > 0)
            {

                BDFDataRecord dataRecord;
                int samplesWritten = 0;
                if (!this.Header.IsBDFPlus)
                {
                    dataRecord = new BDFDataRecord(this.Header.StartDateTime);
                    foreach (BDFSignal signal in this.Header.Signals)
                    {
                        List<double> samples = new List<double>();
                        for (int l = 0; l < signal.NumberOfSamplesPerDataRecord; l++)
                        {
                            byte[] tmp_bytes = new byte[4];
                            tmp_bytes[0] = dataRecordBytes[samplesWritten * 3 + 0];
                            tmp_bytes[1] = dataRecordBytes[samplesWritten * 3 + 1];
                            tmp_bytes[2] = dataRecordBytes[samplesWritten * 3 + 2];
                            tmp_bytes[3] = 0x00;
                            double value = (double)(((getValueSigned(tmp_bytes[2], tmp_bytes[1], tmp_bytes[0]) + (int)signal.Offset)) * signal.AmplifierGain);
                            samples.Add(value);
                            samplesWritten++;
                        }
                        dataRecord.Signals.Add(signal.IndexNumberWithLabel, samples);
                    }
                }
                else
                {
                    dataRecord = new BDFDataRecord(this.Header.StartDateTime);
                    for (int i = 0; i < this.Header.Signals.Count - 1; i++)
                    {
                        BDFSignal signal = this.Header.Signals[i];
                        List<double> samples = new List<double>();
                        for (int l = 0; l < signal.NumberOfSamplesPerDataRecord; l++)
                        {
                            byte[] tmp_bytes = new byte[4];
                            tmp_bytes[0] = dataRecordBytes[samplesWritten * 3 + 0];
                            tmp_bytes[1] = dataRecordBytes[samplesWritten * 3 + 1];
                            tmp_bytes[2] = dataRecordBytes[samplesWritten * 3 + 2];
                            tmp_bytes[3] = 0x00;
                            double value = (double)(((getValueSigned(tmp_bytes[2], tmp_bytes[1], tmp_bytes[0]) + (int)signal.Offset)) * signal.AmplifierGain);
                            samples.Add(value);
                            samplesWritten++;
                        }
                        dataRecord.Signals.Add(signal.IndexNumberWithLabel, samples);
                    }
                    byte[] tmpp = new byte[this.Header.Signals[this.Header.Signals.Count - 1].NumberOfSamplesPerDataRecord * 2];
                    int index = 0;
                    while (index < tmpp.Length)
                    {
                        tmpp[index] = dataRecordBytes[3 * samplesWritten + index];
                        index++;
                    }
                    dataRecord.parseAnnotations(tmpp);
                }
                _dataRecords.Add(dataRecord);

            }

        }
        public void deleteSignal(BDFSignal signal_to_delete)
        {
            if (this.Header.Signals.Contains(signal_to_delete))
            {
                //Remove Signal DataRecords
                foreach (BDFDataRecord dr in this.DataRecords)
                {
                    foreach (BDFSignal signal in this.Header.Signals)
                    {
                        if (signal.IndexNumberWithLabel.Equals(signal_to_delete.IndexNumberWithLabel))
                        {
                            dr.Signals.Remove(signal_to_delete.IndexNumberWithLabel);
                        }
                    }
                }
                //After removing the DataRecords then Remove the Signal from the Header
                this.Header.Signals.Remove(signal_to_delete);

                //Finally Decrement the NumberOfSignals in the Header by 1
                this.Header.NumberOfSignalsInDataRecord = this.Header.NumberOfSignalsInDataRecord - 1;

                //Change the Number Of Bytes in the Header.
                this.Header.NumberOfBytes = (256) + (256 * this.Header.Signals.Count);
            }
        }
        public void addSignal(BDFSignal signal_to_add, List<double> sampleValues)
        {

            if (this.Header.Signals.Contains(signal_to_add))
            {
                this.deleteSignal(signal_to_add);
            }

            //Remove Signal DataRecords
            int index = 0;
            foreach (BDFDataRecord dr in this.DataRecords)
            {
                dr.Signals.Add(signal_to_add.IndexNumberWithLabel, sampleValues.GetRange(index * signal_to_add.NumberOfSamplesPerDataRecord, signal_to_add.NumberOfSamplesPerDataRecord));
                index++;
            }
            //After removing the DataRecords then Remove the Signal from the Header
            this.Header.Signals.Add(signal_to_add);

            //Finally Decrement the NumberOfSignals in the Header by 1
            this.Header.NumberOfSignalsInDataRecord = this.Header.NumberOfSignalsInDataRecord + 1;

            //Change the Number Of Bytes in the Header.
            this.Header.NumberOfBytes = (256) + (256 * this.Header.Signals.Count);

        }
        public List<double> retrieveSignalSampleValues(BDFSignal signal_to_retrieve)
        {
            List<double> signalSampleValues = new List<double>();

            if (this.Header.Signals.Contains(signal_to_retrieve))
            {
                //Remove Signal DataRecords
                foreach (BDFDataRecord dr in this.DataRecords)
                {
                    foreach (BDFSignal signal in this.Header.Signals)
                    {
                        if (signal.IndexNumberWithLabel.Equals(signal_to_retrieve.IndexNumberWithLabel))
                        {
                            foreach (double value in dr.Signals[signal.IndexNumberWithLabel])
                            {
                                signalSampleValues.Add(value);
                            }
                        }
                    }
                }
            }
            return signalSampleValues;

        }
        public void exportAsCompumedics(string file_path)
        {
            foreach (BDFSignal signal in this.Header.Signals)
            {
                string signal_name = this.Header.StartDateTime.ToString("MMddyyyy_HHmm", System.Globalization.CultureInfo.InvariantCulture) + "_" + signal.Label;
                string new_path = string.Empty;
                if (file_path.LastIndexOf('/') == file_path.Length)
                {
                    new_path = file_path + signal_name.Replace(' ', '_');
                }
                else
                {
                    new_path = file_path + '/' + signal_name.Replace(' ', '_');
                }

                if (File.Exists(new_path))
                {
                    File.Delete(new_path);
                }
                FileStream newFile = new FileStream(new_path, FileMode.CreateNew, FileAccess.Write);

                StreamWriter sw = new StreamWriter(newFile);

                if (signal.NumberOfSamplesPerDataRecord <= 0)
                {
                    //need to pad it to be sampled every second.
                    sw.WriteLine(signal.Label + " " + "RATE:1.0Hz");
                }
                else
                {
                    sw.WriteLine(signal.Label + " " + "RATE:" + Math.Round((double)(signal.NumberOfSamplesPerDataRecord / this.Header.DurationOfDataRecordInSeconds), 2) + "Hz");
                }

                foreach (BDFDataRecord dataRecord in this.DataRecords)
                {
                    foreach (double sample in dataRecord.Signals[signal.IndexNumberWithLabel])
                    {
                        sw.WriteLine(sample);
                    }

                }
                sw.Flush();

            }


        }
    }

}
