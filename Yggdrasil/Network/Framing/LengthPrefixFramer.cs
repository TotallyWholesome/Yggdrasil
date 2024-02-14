﻿using System;
using System.IO;

namespace Yggdrasil.Network.Framing
{
	/// <summary>
	/// Handles messages prefixed with a single int that determines its
	/// length, incl. the 4 byte frame.
	/// </summary>
	public class LengthPrefixFramer : IMessageFramer
	{
		private readonly byte[] _lengthBuffer;
		private byte[] _messageBuffer;
		private int _bytesReceived;

		/// <summary>
		/// Maximum size of messages.
		/// </summary>
		public int MaxMessageSize { get; private set; }

		/// <summary>
		/// Called every time ReceiveData got a full message.
		/// </summary>
		public event Action<byte[]> MessageReceived;

		/// <summary>
		/// Creates new instance.
		/// </summary>
		/// <param name="maxMessageSize">Maximum size of messages</param>
		public LengthPrefixFramer(int maxMessageSize)
		{
			this.MaxMessageSize = maxMessageSize;
			_lengthBuffer = new byte[sizeof(int)];
		}

		/// <summary>
		/// Wraps message in length prefixed frame.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		/// <example>
		/// .Frame(new byte[] { 1, 2, 3, 4, 5 }); // byte[] { 9, 0, 0, 0, 1, 2, 3, 4, 5 }
		/// </example>
		public byte[] Frame(byte[] message)
		{
			var messageLength = message.Length;
			if (messageLength > this.MaxMessageSize)
				throw new InvalidMessageSizeException("Invalid size (" + messageLength + ").");

			var dataLength = sizeof(int) + message.Length;
			var result = new byte[dataLength];

			result[0] = (byte)(dataLength >> (8 * 0));
			result[1] = (byte)(dataLength >> (8 * 1));
			result[2] = (byte)(dataLength >> (8 * 2));
			result[3] = (byte)(dataLength >> (8 * 3));

			Buffer.BlockCopy(message, 0, result, 4, messageLength);

			return result;
		}
		
		/// <summary>
		/// Wraps message in length prefixed frame.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public ArraySegment<byte> FrameNoAlloc(MemoryStream message)
		{
			var messageLength = message.Length;
			if (messageLength > this.MaxMessageSize)
				throw new InvalidMessageSizeException("Invalid size (" + messageLength + ").");
			
			message.Position = 0;
			message.Write(BitConverter.GetBytes((int)messageLength), 0, 4);
			return message.TryGetBuffer(out var buffer ) ? buffer : throw new Exception("Failed to get array segment buffer for network frame");
		}

		/// <summary>
		/// Receives data and calls MessageReceived every time a full message
		/// has arrived.
		/// </summary>
		/// <param name="data">Buffer to read from.</param>
		/// <param name="length">Length of actual information in data.</param>
		/// <exception cref="InvalidMessageSizeException">
		/// Thrown if a message has an invalid size. Should this occur,
		/// the connection should be terminated, because it's not save to
		/// keep receiving anymore.
		/// </exception>
		public void ReceiveData(byte[] data, int length)
		{
			var bytesAvailable = length;
			if (bytesAvailable == 0)
				return;

			for (var i = 0; i < bytesAvailable;)
			{
				if (_messageBuffer == null)
				{
					var read = Math.Min(_lengthBuffer.Length - _bytesReceived, bytesAvailable - i);
					Buffer.BlockCopy(data, i, _lengthBuffer, _bytesReceived, read);

					_bytesReceived += read;
					i += read;

					if (_bytesReceived == _lengthBuffer.Length)
					{
						var messageSize = 0;
						messageSize |= _lengthBuffer[0] << (8 * 0);
						messageSize |= _lengthBuffer[1] << (8 * 1);
						messageSize |= _lengthBuffer[2] << (8 * 2);
						messageSize |= _lengthBuffer[3] << (8 * 3);
						messageSize -= sizeof(int);

						if (messageSize < 0 || messageSize > this.MaxMessageSize)
							throw new InvalidMessageSizeException("Invalid size (" + messageSize + ").");

						_messageBuffer = new byte[messageSize];
						_bytesReceived = 0;
					}
				}

				if (_messageBuffer != null)
				{
					var read = Math.Min(_messageBuffer.Length - _bytesReceived, bytesAvailable - i);
					Buffer.BlockCopy(data, i, _messageBuffer, _bytesReceived, read);

					_bytesReceived += read;
					i += read;

					if (_bytesReceived == _messageBuffer.Length)
					{
						this.MessageReceived?.Invoke(_messageBuffer);

						_messageBuffer = null;
						_bytesReceived = 0;
					}
				}
			}
		}
	}

	/// <summary>
	/// An exception that might occur if a message has an invalid size.
	/// </summary>
	public class InvalidMessageSizeException : Exception
	{
		/// <summary>
		/// Creates new instance.
		/// </summary>
		/// <param name="message"></param>
		public InvalidMessageSizeException(string message)
			: base(message)
		{
		}
	}
}
