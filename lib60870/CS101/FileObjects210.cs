using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib60870.CS101
{
    public enum AdditionalPacketType : byte
    {
        RESERVE_1 = 1,
        FILE_TRANSFER = 2,
        RESERVE_3 = 3,
        RESERVE_4 = 4,
    }
    public enum OptionID : byte
    {
        CALL_FILES = 1,//DIRECTORY
        CALL_FILES_CON = 2,
        READ_FILE_ACT = 3,
        READ_FILE_ACT_CON = 4,
        READ_FILE_TRANSFER = 5,
        READ_FILE_TRANSFER_CON = 6,
        WRITE_FILE_ACT = 7,
        WRITE_FILE_ACT_CON = 8,
        WRITE_FILE_TRANSFER = 9,
        WRITE_FILE_TRANSFER_CON = 10,
    }
    public enum CallFlag : byte
    {
        ALL_FILE = 0,
        MATCHED_FILE = 1
    }
    public enum WriteFileActConResult : byte
    {
        SUCCESS = 0,
        UNKNOWN = 1,
        FILE_NAME_UNSUPPORT = 2,
        LENGTH_OUT_OF_RANGE = 3
    }
    public enum WriteFileTransferConResult : byte
    {
        SUCCESS = 0,
        UNKNOWN = 1,
        WRONG_CS = 2,
        FILE_LENGTH_NOT_MATCH = 3,
        FILE_ID_NOT_MATCH = 4
    }
    public class FileInfo
    {
        public string Name = string.Empty;
        public byte Attribute;
        public uint Size;
        public CP56Time2a Date;

        public byte[] GetEncodedValue()
        {
            List<byte> encode = new List<byte>();
            encode.Add((byte)Name.Length);

            if (Name.Length > 0)
            {
                for (int i = 0; i < Name.Length; i++)
                {
                    encode.Add((byte)Name[i]);
                }
            }

            encode.Add(Attribute);

            encode.Add((byte)(Size & 0xff));
            encode.Add((byte)(Size / 0x100 & 0xff));
            encode.Add((byte)(Size / 0x10000 & 0xff));
            encode.Add((byte)(Size / 0x1000000 & 0xff));

            encode.AddRange(Date.GetEncodedValue());

            return encode.ToArray();
        }
    }
    public class FileObjects210 : InformationObject
    {
        public override int GetEncodedSize()
        {
            return 4;
        }
        private OptionID _opt;
        public OptionID OPT {
            get { return _opt; }
        }
        private AdditionalPacketType _additionalPacketType;
        public AdditionalPacketType AdditionalPacketType {
            get { return _additionalPacketType; }
        }

        public FileObjects210(int objectAddress, AdditionalPacketType additionalPacketType, OptionID opt) : base(0)
        {
            _additionalPacketType = additionalPacketType;
            _opt = opt;
        }

        internal FileObjects210(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex, false)
        {
            if (msg.Length - startIndex < GetEncodedSize())
                throw new ASDUParsingException("Message to short");

            ObjectAddress = msg[startIndex];
            ObjectAddress += (msg[startIndex + 1] * 0x100);

            _additionalPacketType = (AdditionalPacketType)msg[startIndex + 2];
            _opt = (OptionID)msg[startIndex + 3];
        }

        public override bool SupportsSequence => false;

        public override TypeID Type => TypeID.F_FR_NA_1_210;

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            frame.SetNextByte((byte)(ObjectAddress & 0xff));
            frame.SetNextByte((byte)((ObjectAddress / 0x100) & 0xff));
            frame.SetNextByte((byte)_additionalPacketType);
            frame.SetNextByte((byte)_opt);
        }
    }
    /// <summary>
    /// OPT = 1
    /// </summary>
    public class FileCall : FileObjects210
    {
        private int _folderId;
        private byte _folderNameLength;
        private string _folderName;
        private CallFlag _callFlag;
        private CP56Time2a _startTime;
        private CP56Time2a _endTime;

        public FileCall(int objectAddress, int folderId, string folderName, CallFlag callFlag, CP56Time2a startTime, CP56Time2a endTime) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.CALL_FILES)
        {
            _folderId = folderId;
            _folderNameLength = (byte)(folderName == null ? 0 : folderName.Length);
            _folderName = folderName;
            _callFlag = callFlag;
            _startTime = startTime;
            _endTime = endTime;
        }

        public FileCall(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _folderId = msg[startIndex++];
            _folderId += msg[startIndex++] * 0x100;
            _folderId += msg[startIndex++] * 0x10000;
            _folderId += msg[startIndex++] * 0x1000000;

            _folderNameLength = msg[startIndex++];

            for (int i = 0; i < _folderNameLength; i++)
            {
                _folderName += (char)msg[startIndex++];
            }

            if ((msg.Length - startIndex) < 15)
                throw new ASDUParsingException("Message too small");

            _callFlag = (CallFlag)msg[startIndex++];

            _startTime = new CP56Time2a(msg, startIndex);
            startIndex += 7;
            _endTime = new CP56Time2a(msg, startIndex);

        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte((byte)(_folderId & 0xff));
            frame.SetNextByte((byte)((_folderId / 0x100) & 0xff));
            frame.SetNextByte((byte)((_folderId / 0x10000) & 0xff));
            frame.SetNextByte((byte)((_folderId / 0x1000000) & 0xff));
            frame.SetNextByte(_folderNameLength);
            if (_folderNameLength > 0)
            {
                foreach (char item in _folderName)
                {
                    frame.SetNextByte((byte)item);
                }
            }
            frame.SetNextByte((byte)_callFlag);
            frame.AppendBytes(_startTime.GetEncodedValue());
            frame.AppendBytes(_endTime.GetEncodedValue());
        }

        public override int GetEncodedSize()
        {
            return 21;
        }
    }
    /// <summary>
    /// OPT = 2
    /// </summary>
    public class FileCallCon : FileObjects210
    {
        private int _folderId;

        private bool _followUp;

        private bool _result;
        public bool Result {
            get { return _result; }
        }
        private byte _fileCount;
        public int FileCount {
            get { return _fileCount; }
        }
        private List<FileInfo> _files;
        public List<FileInfo> Files {
            get { return _files; }
        }

        public FileCallCon(int objectAddress, bool result, int folderId, bool followUp, byte fileCount, List<FileInfo> files) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.CALL_FILES_CON)
        {
            _result = result;
            _folderId = folderId;
            _followUp = followUp;
            _fileCount = fileCount;
            _files = files;
        }

        public FileCallCon(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _result = msg[startIndex++] == 0;

            _folderId = msg[startIndex++];
            _folderId += msg[startIndex++] * 0x100;
            _folderId += msg[startIndex++] * 0x10000;
            _folderId += msg[startIndex++] * 0x1000000;

            _followUp = msg[startIndex++] == 1;//TODO:后续?

            _fileCount = msg[startIndex++];

            _files = new List<FileInfo>();

            for (int i = 0; i < _fileCount; i++)
            {
                var afile = new FileInfo();
                int lengthOfName = msg[startIndex++];
                for (int iname = 0; iname < lengthOfName; iname++)
                {
                    afile.Name += (char)msg[startIndex++];
                }

                afile.Attribute = msg[startIndex++];

                afile.Size = msg[startIndex++];
                afile.Size += (uint)msg[startIndex++] * 0x100;
                afile.Size += (uint)msg[startIndex++] * 0x10000;
                afile.Size += (uint)msg[startIndex++] * 0x1000000;

                afile.Date = new CP56Time2a(msg, startIndex);

                _files.Add(afile);
            }
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte((byte)(_result ? 0 : 1));

            frame.SetNextByte((byte)(_folderId & 0xff));
            frame.SetNextByte((byte)((_folderId / 0x100) & 0xff));
            frame.SetNextByte((byte)((_folderId / 0x10000) & 0xff));
            frame.SetNextByte((byte)((_folderId / 0x1000000) & 0xff));

            frame.SetNextByte((byte)(_followUp ? 1 : 0));

            frame.SetNextByte(_fileCount);
            for (int i = 0; i < _fileCount; i++)
            {
                frame.AppendBytes(_files[i].GetEncodedValue());
            }

        }

        public override int GetEncodedSize()
        {
            return 8;
        }
    }
    /// <summary>
    /// OPT = 3
    /// </summary>
    public class FileReadAct : FileObjects210
    {
        private byte _fileNameLength;
        private string _fileName;
        public FileReadAct(int objectAddress, string fileName) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.READ_FILE_ACT)
        {
            _fileName = fileName;
            _fileNameLength = (byte)(_fileName == null ? 0 : _fileName.Length);
        }

        public FileReadAct(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _fileNameLength = msg[startIndex++];

            _fileName = "";
            for (int i = 0; i < _fileNameLength; i++)
            {
                _fileName += (char)msg[startIndex++];
            }
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte(_fileNameLength);

            for (int i = 0; i < _fileNameLength; i++)
            {
                frame.SetNextByte((byte)_fileName[i]);
            }
        }

        public override int GetEncodedSize()
        {
            return 2;
        }
    }
    /// <summary>
    /// OPT = 4
    /// </summary>
    public class FileReadActCon : FileObjects210
    {
        private bool _result;
        public bool Result {
            get { return _result; }
        }
        private byte _fileNameLength;
        public int FileNameLength {
            get { return _fileNameLength; }
        }
        private string _fileName;
        public string FileName {
            get { return _fileName; }
        }
        private int _fileId;
        public int FileId {
            get { return _fileId; }
        }
        private uint _fileSize;
        public uint FileSize {
            get { return _fileSize; }
        }

        public FileReadActCon(int objectAddress, bool result, string fileName, int fileId, uint fileSize) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.READ_FILE_ACT_CON)
        {
            _result = result;
            _fileNameLength = (byte)(fileName == null ? 0 : fileName.Length);
            _fileName = fileName;
            _fileId = fileId;
            _fileSize = fileSize;
        }

        public FileReadActCon(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _result = msg[startIndex++] == 0;

            _fileNameLength = msg[startIndex++];

            _fileName = "";
            for (int i = 0; i < _fileNameLength; i++)
            {
                _fileName += (char)msg[startIndex++];
            }

            _fileId = msg[startIndex++];
            _fileId += msg[startIndex++] * 0x100;
            _fileId += msg[startIndex++] * 0x10000;
            _fileId += msg[startIndex++] * 0x1000000;

            _fileSize = msg[startIndex++];
            _fileSize += (uint)msg[startIndex++] * 0x100;
            _fileSize += (uint)msg[startIndex++] * 0x10000;
            _fileSize += (uint)msg[startIndex++] * 0x100000;

        }

        public override int GetEncodedSize()
        {
            return 11;
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte((byte)(_result ? 0 : 1));

            frame.SetNextByte(_fileNameLength);

            for (int i = 0; i < _fileNameLength; i++)
            {
                frame.SetNextByte((byte)_fileName[i]);
            }

            frame.SetNextByte((byte)(_fileId & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_fileSize & 0xff));
            frame.SetNextByte((byte)(_fileSize / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileSize / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileSize / 0x1000000 & 0xff));
        }
    }
    /// <summary>
    /// OPT = 5
    /// </summary>
    public class FIleReadTransfer : FileObjects210
    {
        private int _fileId;
        public int FileId {
            get { return _fileId; }
        }
        private int _offset;
        public int Offset {
            get { return _offset; }
        }
        private bool _followup;
        public bool Followup {
            get { return _followup; }
        }
        private byte[] _data;
        public byte[] Data {
            get { return _data; }
        }
        private byte _cs;
        public byte CS {
            get { return _cs; }
        }
        public FIleReadTransfer(int objectAddress, int fileId, int offset, bool followup, byte[] data, byte cs) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.READ_FILE_TRANSFER)
        {
            _fileId = fileId;
            _offset = offset;
            _followup = followup;
            _data = data;
            _cs = cs;
        }

        public FIleReadTransfer(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _fileId = msg[startIndex++];
            _fileId += msg[startIndex++] * 0x100;
            _fileId += msg[startIndex++] * 0x10000;
            _fileId += msg[startIndex++] * 0x1000000;

            _offset = msg[startIndex++];
            _offset += msg[startIndex++] * 0x100;
            _offset += msg[startIndex++] * 0x10000;
            _offset += msg[startIndex++] * 0x1000000;

            _followup = msg[startIndex++] == 1;

            if (msg.Length - startIndex - 1 < 0)
                throw new ASDUParsingException("File contents is empty");

            _data = new byte[msg.Length - startIndex - 1];
            Array.Copy(msg, startIndex, _data, 0, _data.Length);

            _cs = msg[msg.Length - 1];
        }

        public override int GetEncodedSize()
        {
            return 11;
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte((byte)(_fileId & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_offset & 0xff));
            frame.SetNextByte((byte)(_offset / 0x100 & 0xff));
            frame.SetNextByte((byte)(_offset / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_offset / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_followup ? 1 : 0));

            frame.AppendBytes(_data);

            frame.SetNextByte(_cs);
        }
    }
    /// <summary>
    /// OPT = 6
    /// </summary>
    public class FIleReadTransferCon : FileObjects210
    {
        private int _fileId;
        private int _offset;
        private bool _followup;
        public FIleReadTransferCon(int objectAddress, int fileId, int offset, bool followup) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.READ_FILE_TRANSFER_CON)
        {
            _fileId = fileId;
            _offset = offset;
            _followup = followup;
        }

        public FIleReadTransferCon(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _fileId = msg[startIndex++];
            _fileId += msg[startIndex++] * 0x100;
            _fileId += msg[startIndex++] * 0x10000;
            _fileId += msg[startIndex++] * 0x1000000;

            _offset = msg[startIndex++];
            _offset += msg[startIndex++] * 0x100;
            _offset += msg[startIndex++] * 0x10000;
            _offset += msg[startIndex++] * 0x1000000;

            _followup = msg[startIndex++] == 1;
        }
        public override int GetEncodedSize()
        {
            return 10;
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte((byte)(_fileId & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_offset & 0xff));
            frame.SetNextByte((byte)(_offset / 0x100 & 0xff));
            frame.SetNextByte((byte)(_offset / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_offset / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_followup ? 1 : 0));

        }
    }
    /// <summary>
    /// OPT = 7
    /// </summary>
    public class FIleWriteAct : FileObjects210
    {
        private byte _fileNameLength;
        public int FileNameLength {
            get { return _fileNameLength; }
        }
        private string _fileName;
        public string FileName {
            get { return _fileName; }
        }
        private int _fileId;
        public int FileId {
            get { return _fileId; }
        }
        private uint _fileSize;
        public uint FileSize {
            get { return _fileSize; }
        }
        public FIleWriteAct(int objectAddress, string fileName, int fileId, uint fileSize) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.WRITE_FILE_ACT)
        {
            _fileNameLength = (byte)(fileName == null ? 0 : fileName.Length);
            _fileName = fileName;
            _fileId = fileId;
            _fileSize = fileSize;
        }

        public FIleWriteAct(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _fileNameLength = msg[startIndex++];

            _fileName = "";
            for (int i = 0; i < _fileNameLength; i++)
            {
                _fileName += (char)msg[startIndex++];
            }

            _fileId = msg[startIndex++];
            _fileId += msg[startIndex++] * 0x100;
            _fileId += msg[startIndex++] * 0x10000;
            _fileId += msg[startIndex++] * 0x1000000;

            _fileSize = msg[startIndex++];
            _fileSize += (uint)msg[startIndex++] * 0x100;
            _fileSize += (uint)msg[startIndex++] * 0x10000;
            _fileSize += (uint)msg[startIndex++] * 0x100000;

        }

        public override int GetEncodedSize()
        {
            return 10;
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte(_fileNameLength);

            for (int i = 0; i < _fileNameLength; i++)
            {
                frame.SetNextByte((byte)_fileName[i]);
            }

            frame.SetNextByte((byte)(_fileId & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_fileSize & 0xff));
            frame.SetNextByte((byte)(_fileSize / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileSize / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileSize / 0x1000000 & 0xff));
        }
    }
    /// <summary>
    /// OPT = 8
    /// </summary>
    public class FIleWriteActCon : FileObjects210
    {
        private WriteFileActConResult _result;
        public WriteFileActConResult Result {
            get { return _result; }
        }
        private byte _fileNameLength;
        public int FileNameLength {
            get { return _fileNameLength; }
        }
        private string _fileName;
        public string FileName {
            get { return _fileName; }
        }
        private int _fileId;
        public int FileId {
            get { return _fileId; }
        }
        private uint _fileSize;
        public uint FileSize {
            get { return _fileSize; }
        }
        public FIleWriteActCon(int objectAddress, WriteFileActConResult result, string fileName, int fileId, uint fileSize) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.WRITE_FILE_ACT_CON)
        {
            _result = result;
            _fileNameLength = (byte)(fileName == null ? 0 : fileName.Length);
            _fileName = fileName;
            _fileId = fileId;
            _fileSize = fileSize;
        }

        public FIleWriteActCon(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _result = (WriteFileActConResult)msg[startIndex++];

            _fileNameLength = msg[startIndex++];

            _fileName = "";
            for (int i = 0; i < _fileNameLength; i++)
            {
                _fileName += (char)msg[startIndex++];
            }

            _fileId = msg[startIndex++];
            _fileId += msg[startIndex++] * 0x100;
            _fileId += msg[startIndex++] * 0x10000;
            _fileId += msg[startIndex++] * 0x1000000;

            _fileSize = msg[startIndex++];
            _fileSize += (uint)msg[startIndex++] * 0x100;
            _fileSize += (uint)msg[startIndex++] * 0x10000;
            _fileSize += (uint)msg[startIndex++] * 0x100000;

        }

        public override int GetEncodedSize()
        {
            return 11;
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte((byte)(_result));

            frame.SetNextByte(_fileNameLength);

            for (int i = 0; i < _fileNameLength; i++)
            {
                frame.SetNextByte((byte)_fileName[i]);
            }

            frame.SetNextByte((byte)(_fileId & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_fileSize & 0xff));
            frame.SetNextByte((byte)(_fileSize / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileSize / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileSize / 0x1000000 & 0xff));
        }
    }
    /// <summary>
    /// OPT = 9
    /// </summary>
    public class FIleWriteTransfer : FileObjects210
    {
        private int _fileId;
        public int FileId {
            get { return _fileId; }
        }
        private int _offset;
        public int Offset {
            get { return _offset; }
        }
        private bool _followup;
        public bool Followup {
            get { return _followup; }
        }
        private byte[] _data;
        public byte[] Data {
            get { return _data; }
        }
        private byte _cs;
        public byte CS {
            get { return _cs; }
        }
        public FIleWriteTransfer(int objectAddress, int fileId, int offset, bool followup, byte[] data, byte cs) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.WRITE_FILE_TRANSFER)
        {
            _fileId = fileId;
            _offset = offset;
            _followup = followup;
            _data = data;
            _cs = cs;
        }

        public FIleWriteTransfer(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _fileId = msg[startIndex++];
            _fileId += msg[startIndex++] * 0x100;
            _fileId += msg[startIndex++] * 0x10000;
            _fileId += msg[startIndex++] * 0x1000000;

            _offset = msg[startIndex++];
            _offset += msg[startIndex++] * 0x100;
            _offset += msg[startIndex++] * 0x10000;
            _offset += msg[startIndex++] * 0x1000000;

            _followup = msg[startIndex++] == 1;

            if (msg.Length - startIndex - 1 < 0)
                throw new ASDUParsingException("File contents is empty");

            _data = new byte[msg.Length - startIndex - 1];
            Array.Copy(msg, startIndex, _data, 0, _data.Length);

            _cs = msg[msg.Length - 1];
        }

        public override int GetEncodedSize()
        {
            return 11;
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte((byte)(_fileId & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_offset & 0xff));
            frame.SetNextByte((byte)(_offset / 0x100 & 0xff));
            frame.SetNextByte((byte)(_offset / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_offset / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_followup ? 1 : 0));

            frame.AppendBytes(_data);

            frame.SetNextByte(_cs);
        }
    }
    /// <summary>
    /// OPT = 10
    /// </summary>
    public class FIleWriteTransferCon : FileObjects210
    {
        private int _fileId;
        public int FileId {
            get { return _fileId; }
        }
        private int _offset;
        public int Offset {
            get { return _offset; }
        }
        private WriteFileTransferConResult _result;
        public WriteFileTransferConResult Result {
            get { return _result; }
        }

        public FIleWriteTransferCon(int objectAddress, int fileId, int offset, WriteFileTransferConResult result) : base(objectAddress, AdditionalPacketType.FILE_TRANSFER, OptionID.WRITE_FILE_TRANSFER_CON)
        {
            _fileId = fileId;
            _offset = offset;
            _result = result;
        }

        public FIleWriteTransferCon(ApplicationLayerParameters parameters, byte[] msg, int startIndex) : base(parameters, msg, startIndex)
        {
            startIndex += 2; /* skip IOA */
            startIndex += 1; /* skip AdditionalPacketType */

            if ((msg.Length - startIndex) < GetEncodedSize())
                throw new ASDUParsingException("Message too small");

            startIndex += 1; /* skip OPT */

            _fileId = msg[startIndex++];
            _fileId += msg[startIndex++] * 0x100;
            _fileId += msg[startIndex++] * 0x10000;
            _fileId += msg[startIndex++] * 0x1000000;

            _offset = msg[startIndex++];
            _offset += msg[startIndex++] * 0x100;
            _offset += msg[startIndex++] * 0x10000;
            _offset += msg[startIndex++] * 0x1000000;

            _result = (WriteFileTransferConResult)msg[startIndex++];
        }
        public override int GetEncodedSize()
        {
            return 10;
        }

        public override void Encode(Frame frame, ApplicationLayerParameters parameters, bool isSequence)
        {
            base.Encode(frame, parameters, isSequence);

            frame.SetNextByte((byte)(_fileId & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x100 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_fileId / 0x1000000 & 0xff));

            frame.SetNextByte((byte)(_offset & 0xff));
            frame.SetNextByte((byte)(_offset / 0x100 & 0xff));
            frame.SetNextByte((byte)(_offset / 0x10000 & 0xff));
            frame.SetNextByte((byte)(_offset / 0x1000000 & 0xff));

            frame.SetNextByte((byte)_result);

        }
    }
}
