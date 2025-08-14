using System;
using System.IO;
using System.IO.Compression;
using System.Text;

public class CompressionUtil {
	// Function to compress a string and return the Base64 representation of the gzipped string
	public static string EncodeGzipBase64(string input) {
		byte[] bytes = EncodeGzipBytes(input);

		return Convert.ToBase64String(bytes);
	}

	public static byte[] EncodeGzipBytes(string input) {
		MemoryStream memoryStream;
		if (input == null)
			throw new ArgumentNullException(nameof(input));

		// Convert the string to a byte array
		byte[] inputBytes = Encoding.UTF8.GetBytes(input);
		memoryStream = new MemoryStream();

		// Create a GZipStream for compression
		using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress)) {
			// Write the input bytes to the GZipStream
			gzipStream.Write(inputBytes, 0, inputBytes.Length);
		}

		// Get the compressed bytes from the memory stream
		return memoryStream.ToArray();
	}

	// Function to decode a Base64 gzipped string and return the decompressed original string
	public static string DecodeGzippedBase64(string base64Gzipped) {
		if (base64Gzipped == null)
			throw new ArgumentNullException(nameof(base64Gzipped));

		byte[] compressedBytes = Convert.FromBase64String(base64Gzipped);

		return DecodeGzipBytes(compressedBytes);
	}

	public static string DecodeGzipBytes(byte[] compressedBytes) {
		MemoryStream memoryStream, resultStream;
		GZipStream gzipStream;

		// Convert the Base64 string to a byte array
		memoryStream = new MemoryStream(compressedBytes);
		gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
		resultStream = new MemoryStream();
		// Decompress the data
		gzipStream.CopyTo(resultStream);

		return Encoding.UTF8.GetString(resultStream.ToArray());
	}
}