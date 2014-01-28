using System;
using System.IO;

namespace FamtChatLibrary
{
    /// <summary>
    /// Class to wrap our data and its type etc.
    /// </summary>
    public class MessageWrapper
    {
        public MessageType MessageType { get; set; }
        public String Data { get; set; }

        public MessageWrapper()
        {
            this.Data = "";
        }
        /// <summary>
        /// Quick binary serialization
        /// </summary>
        public byte[] Serialize()
        {
            using (MemoryStream m = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(m))
                {
                    writer.Write(this.MessageType.ToString());
                    writer.Write(this.Data);
                }
                return m.ToArray();
            }
        }

        /// <summary>
        /// Quick binary Deserialization
        /// </summary>
        public static MessageWrapper Desserialize(byte[] data)
        {
            MessageWrapper result = new MessageWrapper();
            using (MemoryStream m = new MemoryStream(data))
            {
                using (BinaryReader reader = new BinaryReader(m))
                {
                    MessageType fromString;
                    String s = reader.ReadString();
                    if (Enum.TryParse<MessageType>(s, out fromString))
                    {
                        //succeeded
                        result.MessageType = fromString;
                    }
                    else
                    {
                        //not valid
                    }
                    result.Data = reader.ReadString();
                }
            }
            return result;
        }

    }
}
