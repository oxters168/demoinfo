using System;
using System.Collections.Generic;
using System.IO;

namespace DemoInfo.BitStreamImpl
{
    public class BitStreamFromStream : IBitStream
    {
        private Stream stream;

        private readonly static byte[] singularMasks = new byte[] { 0x1, 0x2, 0x4, 0x8, 0x10, 0x20, 0x40, 0x80 };
        private readonly static byte[] bitMasks = new byte[] { 0xff, 0xfe, 0xfc, 0xf8, 0xf0, 0xe0, 0xc0, 0x80 };

        /// <summary>
        /// Bit position index in stream
        /// </summary>
        public long Position { get { return _Position; } set { _Position = Math.Min(Math.Max(0, value), stream.Length * 8); stream.Position = _Position / 8; } }
        private long _Position;

        private Stack<long> chunkTargets = new Stack<long>();
        public bool ChunkFinished => Position >= chunkTargets.Peek();

        public BitStreamFromStream(Stream _stream)
        {
            Initialize(_stream);
        }
        public void Dispose()
        {
            stream = null;
        }

        public void BeginChunk(int bits)
        {
            chunkTargets.Push(Position + bits);
        }
        public void EndChunk()
        {
            Position = chunkTargets.Pop();
        }

        public void Initialize(Stream _stream)
        {
            stream = _stream;
            _Position = stream.Position * 8;
        }

        public bool ReadBit()
        {
            byte currentByte = currentByte = DataParser.ReadByte(stream);

            int maskIndex = (int)(Position % 8);
            Position += 1;

            return (currentByte & singularMasks[maskIndex]) != 0;
        }

        public byte[] ReadBits(int bits)
        {
            byte[] outputBytes = new byte[(bits / 8) + (bits % 8 > 0 ? 1 : 0)];
            byte bitOffset = (byte)(Position % 8);
            byte[] data = new byte[outputBytes.Length + 1];
            stream.Read(data, 0, data.Length);

            int bitsRead = 0;
            for (int outputByteIndex = 0; outputByteIndex < outputBytes.Length; outputByteIndex++)
            {
                //Start reading bits of current byte
                outputBytes[outputByteIndex] |= (byte)((data[outputByteIndex] & bitMasks[bitOffset]) >> bitOffset);
                bitsRead += (byte)(8 - bitOffset);

                //If we did not get an entire byte, continue reading bits
                if (bitsRead < bits && bitOffset > 0)
                {
                    outputBytes[outputByteIndex] |= (byte)((data[outputByteIndex + 1] & ~bitMasks[bitOffset]) << (8 - bitOffset));
                    bitsRead += bitOffset;
                }
                //Trim off excess bits
                if (bitsRead > bits)
                    outputBytes[outputByteIndex] &= (byte)~bitMasks[bits % 8];
            }

            Position += bits;

            return outputBytes;
        }

        public byte ReadByte()
        {
            return ReadBits(8)[0];
        }

        public byte ReadByte(int bits)
        {
            BitStreamUtil.AssertMaxBits(8, bits);
            return ReadBits(bits)[0];
        }

        public byte[] ReadBytes(int bytes)
        {
            return ReadBits(bytes * 8);
        }

        public float ReadFloat()
        {
            return BitConverter.ToSingle(ReadBits(32), 0);
        }

        public uint ReadInt(int bits)
        {
            byte[] fullArray = new byte[4];
            byte[] readBytes = ReadBits(bits);
            Array.Copy(readBytes, fullArray, readBytes.Length);
            return BitConverter.ToUInt32(fullArray, 0);
        }

        public int ReadProtobufVarInt()
        {
            return BitStreamUtil.ReadProtobufVarIntStub(this);
        }

        public int ReadSignedInt(int numBits)
        {
            byte[] fullArray = new byte[4];
            byte[] readBytes = ReadBits(numBits);
            Array.Copy(readBytes, fullArray, readBytes.Length);
            return BitConverter.ToInt32(fullArray, 0);
        }
    }
}
