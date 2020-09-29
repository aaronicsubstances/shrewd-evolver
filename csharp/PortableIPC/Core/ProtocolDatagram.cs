using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PortableIPC.Core
{
    public class ProtocolDatagram
    {
        public const byte OpCodeOpen = 1;
        public const byte OpCodeData = 2;
        public const byte OpCodeAck = 3;
        public const byte OpCodeClose = 4;
        public const byte OpCodeError = 5;
        public const byte OpCodeCloseAll = 6;

        public const byte ChecksumTypeNull = 0;
        public const byte ChecksumTypeLength = 1;
        public const byte ChecksumTypeLRC = 2;
        public const byte ChecksumTypeLengthAndLRC = 3;

        private const byte NullTerminator = 0;

        public const int SessionIdLength = 50;

        // opCode, sessionId, sequence number, checksum_type, null terminator are always present.
        private const int MinDatagramSize = 1 + SessionIdLength + 4*2 + 1 + 1;

        private const string OptionNameRetryCount = "retry_count";
        private const string OptionNameWindowSize = "window_size";
        private const string OptionNameIdleTimeoutMillis = "idle_timeout_millis";
        private const string OptionNameAckTimeoutMillis = "ack_timeout_millis";
        private const string OptionNameErrorCode = "error_code";
        private const string OptionNameErrorMessage = "error_message";

        public byte OpCode { get; set; }
        public string SessionId { get; set; }
        public int SequenceNumberRangeStart { get; set; }
        public int SequenceNumberRangeEnd { get; set; }
        public byte ChecksumType { get; set; }
        public Dictionary<string, List<string>> RemainingOptions { get; set; }
        public byte[] DataBytes { get; set; }
        public int DataOffset { get; set; }
        public int DataLength { get; set; }

        // Known options.
        public short? RetryCount { get; set; }
        public short? WindowSize { get; set; }
        public int? AckTimeoutMillis { get; set; }
        public int? IdleTimeoutMillis { get; set; }
        public short? ErrorCode { get; set; }
        public string ErrorMessage { get; set; }

        public static ProtocolDatagram Parse(byte[] rawBytes, int offset, int length)
        {
            if (length < MinDatagramSize)
            {
                throw new Exception("datagram too small to be valid");
            }

            var parsedDatagram = new ProtocolDatagram();
            int endOffset = offset + length;

            parsedDatagram.OpCode = rawBytes[offset];
            offset += 1;

            parsedDatagram.SessionId = ConvertBytesToString(rawBytes, offset, SessionIdLength);
            offset += SessionIdLength;

            parsedDatagram.SequenceNumberRangeStart = ReadInt32BigEndian(rawBytes, offset);
            offset += 4;

            parsedDatagram.SequenceNumberRangeEnd = ReadInt32BigEndian(rawBytes, offset);
            offset += 4;

            parsedDatagram.ChecksumType = rawBytes[offset];
            offset += 1;

            // Now read options until we encounter null terminator for all options, 
            // which is equivalent to empty string option name 
            string optionName = null;
            while (optionName != "")
            {
                // look for null terminator.
                int nullTerminatorIndex = -1;
                for (int i = offset; i < endOffset; i++)
                {
                    if (rawBytes[i] == NullTerminator)
                    {
                        nullTerminatorIndex = i;
                        break;
                    }
                }
                if (nullTerminatorIndex == -1)
                {
                    throw new Exception("null terminator for all options not found");
                }

                var optionNameOrValue = ConvertBytesToString(rawBytes, offset, nullTerminatorIndex);
                offset = nullTerminatorIndex + 1;

                if (optionName == null)
                {
                    optionName = optionNameOrValue;
                }
                else
                {
                    // Identify known options first. In case of repetition, first one wins.
                    bool knownOptionEncountered = true;
                    switch (optionName)
                    {
                        case OptionNameRetryCount:
                            if (parsedDatagram.RetryCount == null)
                            {
                                parsedDatagram.RetryCount = ParseOptionAsInt16(optionName, optionNameOrValue);
                            }
                            break;
                        case OptionNameWindowSize:
                            if (parsedDatagram.WindowSize == null)
                            {
                                parsedDatagram.WindowSize = ParseOptionAsInt16(optionName, optionNameOrValue);
                            }
                            break;
                        case OptionNameIdleTimeoutMillis:
                            if (parsedDatagram.IdleTimeoutMillis == null)
                            {
                                parsedDatagram.IdleTimeoutMillis = ParseOptionAsInt32(optionName, optionNameOrValue);
                            }
                            break;
                        case OptionNameAckTimeoutMillis:
                            if (parsedDatagram.AckTimeoutMillis == null)
                            {
                                parsedDatagram.AckTimeoutMillis = ParseOptionAsInt32(optionName, optionNameOrValue);
                            }
                            break;
                        case OptionNameErrorCode:
                            if (parsedDatagram.ErrorCode == null)
                            {
                                parsedDatagram.ErrorCode = ParseOptionAsInt16(optionName, optionNameOrValue);
                            }
                            break;
                        case OptionNameErrorMessage:
                            if (parsedDatagram.ErrorMessage == null)
                            {
                                parsedDatagram.ErrorMessage = optionNameOrValue;
                            }
                            break;
                        default:
                            knownOptionEncountered = false;
                            break;
                    }
                    if (!knownOptionEncountered)
                    {
                        if (parsedDatagram.RemainingOptions == null)
                        {
                            parsedDatagram.RemainingOptions = new Dictionary<string, List<string>>();
                        }
                        List<string> optionValues;
                        if (parsedDatagram.RemainingOptions.ContainsKey(optionName))
                        {
                            optionValues = parsedDatagram.RemainingOptions[optionName];
                        }
                        else
                        {
                            optionValues = new List<string>();
                            parsedDatagram.RemainingOptions.Add(optionName, optionValues);
                        }
                        optionValues.Add(optionNameOrValue);
                    }
                    optionName = null;
                }
            }

            parsedDatagram.DataBytes = rawBytes;
            parsedDatagram.DataOffset = offset;
            parsedDatagram.DataLength = endOffset - offset;

            int checksumLength = ValidateChecksum(rawBytes, offset, parsedDatagram.DataLength, parsedDatagram.ChecksumType);
            parsedDatagram.DataLength -= checksumLength;

            return parsedDatagram;
        }

        public byte[] ToRawDatagram()
        {
            byte[] rawBytes;
            int checksumLength = DetermineChecksumLength(ChecksumType);
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(OpCode);
                    byte[] sessionId = ConvertStringToBytes(SessionId);
                    if (sessionId.Length != SessionIdLength)
                    {
                        throw new Exception($"Received invalid session id: {SessionId} produces {sessionId.Length} bytes");
                    }
                    writer.Write(sessionId);
                    WriteInt32BigEndian(writer, SequenceNumberRangeStart);
                    WriteInt32BigEndian(writer, SequenceNumberRangeEnd);
                    writer.Write(ChecksumType);

                    // write out all options, starting with known ones.
                    var knownOptions = new Dictionary<string, string>();
                    if (RetryCount != null)
                    {
                        knownOptions.Add(OptionNameRetryCount, RetryCount.ToString());
                    }
                    if (WindowSize != null)
                    {
                        knownOptions.Add(OptionNameWindowSize, WindowSize.ToString());
                    }
                    if (IdleTimeoutMillis != null)
                    {
                        knownOptions.Add(OptionNameIdleTimeoutMillis, IdleTimeoutMillis.ToString());
                    }
                    if (AckTimeoutMillis != null)
                    {
                        knownOptions.Add(OptionNameAckTimeoutMillis, AckTimeoutMillis.ToString());
                    }
                    if (ErrorCode != null)
                    {
                        knownOptions.Add(OptionNameErrorCode, ErrorCode.ToString());
                    }
                    if (ErrorMessage != null)
                    {
                        knownOptions.Add(OptionNameErrorMessage, ErrorMessage);
                    }
                    foreach (var kvp in knownOptions)
                    {
                        var optionNameBytes = ConvertStringToBytes(kvp.Key);
                        writer.Write(optionNameBytes);
                        writer.Write(NullTerminator);
                        var optionValueBytes = ConvertStringToBytes(kvp.Value);
                        writer.Write(optionValueBytes);
                        writer.Write(NullTerminator);
                    }
                    if (RemainingOptions != null)
                    {
                        foreach (var kvp in RemainingOptions)
                        {
                            var optionNameBytes = ConvertStringToBytes(kvp.Key);
                            foreach (var optionValue in kvp.Value)
                            {
                                writer.Write(optionNameBytes);
                                writer.Write(NullTerminator);
                                var optionValueBytes = ConvertStringToBytes(optionValue);
                                writer.Write(optionValueBytes);
                                writer.Write(NullTerminator);
                            }
                        }
                    }

                    writer.Write(NullTerminator);
                    if (DataBytes != null)
                    {
                        writer.Write(DataBytes, DataOffset, DataLength);
                    }
                    for (int i = 0; i < checksumLength; i++)
                    {
                        writer.Write((byte) 0);
                    }
                }
                rawBytes = ms.ToArray();
            }
            InsertChecksum(rawBytes, ChecksumType);
            return rawBytes;
        }

        internal static int DetermineChecksumLength(short checksumType)
        {
            switch (checksumType)
            {
                case ChecksumTypeNull:
                    return 0;
                case ChecksumTypeLength:
                    return 2;
                case ChecksumTypeLRC:
                    return 1;
                case ChecksumTypeLengthAndLRC:
                    return 3;
                default:
                    throw new Exception("Unknown checksum type: " + checksumType);
            }
        }

        internal static void InsertChecksum(byte[] rawBytes, short checksumType)
        {
            switch (checksumType)
            {
                case ChecksumTypeNull:
                    return;
                case ChecksumTypeLength:
                    WriteInt16BigEndian(rawBytes, rawBytes.Length - 2, (short)(rawBytes.Length - 2));
                    break;
                case ChecksumTypeLRC:
                    rawBytes[rawBytes.Length - 1] = CalculateLongitudinalParityCheck(rawBytes, 0, rawBytes.Length - 1);
                    break;
                case ChecksumTypeLengthAndLRC:
                    WriteInt16BigEndian(rawBytes, rawBytes.Length - 3, (short)(rawBytes.Length - 3));
                    rawBytes[rawBytes.Length - 1] = CalculateLongitudinalParityCheck(rawBytes, 0, rawBytes.Length - 3);
                    break;
                default:
                    throw new Exception("Unknown checksum type: " + checksumType);
            }
        }

        private static int ValidateChecksum(byte[] rawBytes, int offset, int length, short checksumType)
        {
            int checksumLength = DetermineChecksumLength(checksumType);
            if (length < checksumLength)
            {
                throw new Exception("received truncated message: couldn't find checksum.");
            }
            int expectedLen, expectedLrc;
            switch (checksumType)
            {
                case ChecksumTypeNull:
                    return 0;
                case ChecksumTypeLength:
                    expectedLen = ReadInt16BigEndian(rawBytes, offset + length - 2);
                    if (length != expectedLen)
                    {
                        throw new Exception("checksum error");
                    }
                    break;
                case ChecksumTypeLRC:
                    expectedLrc = CalculateLongitudinalParityCheck(rawBytes, offset, length - 1);
                    if (rawBytes[offset + length - 1] != expectedLrc)
                    {
                        throw new Exception("checksum error");
                    }
                    break;
                case ChecksumTypeLengthAndLRC:
                    expectedLen = ReadInt16BigEndian(rawBytes, offset + length - 3);
                    if (length != expectedLen)
                    {
                        throw new Exception("checksum error");
                    }
                    expectedLrc = CalculateLongitudinalParityCheck(rawBytes, offset, length - 3);
                    if (rawBytes[offset + length - 1] != expectedLrc)
                    {
                        throw new Exception("checksum error");
                    }
                    break;
                default:
                    throw new Exception("Unknown checksum type: " + checksumType);
            }
            return checksumLength;
        }

        internal static byte CalculateLongitudinalParityCheck(byte[] byteData, int offset, int length)
        {
            byte chkSumByte = 0x00;
            for (int i = offset; i < offset + length; i++)
            {
                chkSumByte ^= byteData[i];
            }
            return chkSumByte;
        }

        internal static short ParseOptionAsInt16(string optionName, string optionValue)
        {
            if (short.TryParse(optionValue, out short val))
            {
                return val;
            }
            throw new Exception($"Received invalid value for option {optionName}: {optionValue}");
        }

        internal static int ParseOptionAsInt32(string optionName, string optionValue)
        {
            if (int.TryParse(optionValue, out int val))
            {
                return val;
            }
            throw new Exception($"Received invalid value for option {optionName}: {optionValue}");
        }

        internal static byte[] ConvertStringToBytes(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        internal static string ConvertBytesToString(byte[] data, int offset, int length)
        {
            return Encoding.UTF8.GetString(data, offset, length);
        }

        internal static void WriteInt16BigEndian(BinaryWriter writer, short v)
        {
            writer.Write((byte)(0xff & (v >> 8)));
            writer.Write((byte)(0xff & v));
        }

        internal static void WriteInt16BigEndian(byte[] rawBytes, int offset, short v)
        {
            rawBytes[offset] = (byte)(0xff & (v >> 8));
            rawBytes[offset + 1] = (byte)(0xff & v);
        }

        internal static void WriteInt32BigEndian(BinaryWriter writer, int v)
        {
            writer.Write((byte)(0xff & (v >> 24)));
            writer.Write((byte)(0xff & (v >> 16)));
            writer.Write((byte)(0xff & (v >> 8)));
            writer.Write((byte)(0xff & v));
        }

        internal static short ReadInt16BigEndian(byte[] rawBytes, int offset)
        {
            byte a = rawBytes[offset];
            byte b = rawBytes[offset + 1];
            int v = (a << 8) | (b & 0xff);
            return (short)v;
        }

        internal static int ReadInt32BigEndian(byte[] rawBytes, int offset)
        {
            byte a = rawBytes[offset];
            byte b = rawBytes[offset + 1];
            byte c = rawBytes[offset + 2];
            byte d = rawBytes[offset + 3];
            int v = ((a & 0xff) << 24) | ((b & 0xff) << 16) |
                ((c & 0xff) << 8) | (d & 0xff);
            return v;
        }
    }
}
