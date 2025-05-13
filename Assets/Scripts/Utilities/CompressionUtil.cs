using System;
using System.IO;
using System.IO.Compression;
using System.Text;

public class CompressionUtil {
	// Function to compress a string and return the Base64 representation of the gzipped string
	public static string GetGzippedBase64(string input) {
		if (input == null)
			throw new ArgumentNullException(nameof(input));

		// Convert the string to a byte array
		byte[] inputBytes = Encoding.UTF8.GetBytes(input);

		// Create a memory stream to hold the gzipped data
		using var memoryStream = new MemoryStream();

		// Create a GZipStream for compression
		using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress)) {
			// Write the input bytes to the GZipStream
			gzipStream.Write(inputBytes, 0, inputBytes.Length);
		}

		// Get the compressed bytes from the memory stream
		byte[] compressedBytes = memoryStream.ToArray();

		// Convert the compressed bytes to Base64
		return Convert.ToBase64String(compressedBytes);
	}

	// Function to decode a Base64 gzipped string and return the decompressed original string
	public static string DecodeGzippedBase64(string base64Gzipped) {
		if (base64Gzipped == null)
			throw new ArgumentNullException(nameof(base64Gzipped));

		// Convert the Base64 string to a byte array
		byte[] compressedBytes = Convert.FromBase64String(base64Gzipped);

		// Create a memory stream from the Base64 decoded bytes
		using var memoryStream = new MemoryStream(compressedBytes);
		
		// Create a GZipStream for decompression
		using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
		
		using var resultStream = new MemoryStream();
		// Decompress the data
		gzipStream.CopyTo(resultStream);

		// Convert the decompressed data back to a string
		return Encoding.UTF8.GetString(resultStream.ToArray());
	}
}