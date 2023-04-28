//	Copyright (c) 2012 Calvin Rien
//        http://the.darktable.com
//
//	This software is provided 'as-is', without any express or implied warranty. In
//	no event will the authors be held liable for any damages arising from the use
//	of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it freely,
//	subject to the following restrictions:
//
//	1. The origin of this software must not be misrepresented; you must not claim
//	that you wrote the original software. If you use this software in a product,
//	an acknowledgment in the product documentation would be appreciated but is not
//	required.
//
//	2. Altered source versions must be plainly marked as such, and must not be
//	misrepresented as being the original software.
//
//	3. This notice may not be removed or altered from any source distribution.
//
//  =============================================================================
//
//  derived from Gregorio Zanon's script
//  http://forum.unity3d.com/threads/119295-Writing-AudioListener.GetOutputData-to-wav-problem?p=806734&viewfull=1#post806734

// This has been modified by ChatGPT on 2023-04-27

using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public static class WavUtility
{
	const int HEADER_SIZE = 44;

	public static byte[] FromAudioClip(AudioClip clip)
	{
		using (var stream = new MemoryStream())
		{
			WriteHeader(stream, clip);
			ConvertAndWrite(stream, clip);
			return stream.ToArray();
		}
	}

	static void ConvertAndWrite(MemoryStream stream, AudioClip clip)
	{
		var samples = new float[clip.samples];
		clip.GetData(samples, 0);

		Int16[] intData = new Int16[samples.Length];
		Byte[] bytesData = new Byte[samples.Length * 2];
		int rescaleFactor = 32767;

		for (int i = 0; i < samples.Length; i++)
		{
			intData[i] = (short)(samples[i] * rescaleFactor);
			Byte[] byteArr = new Byte[2];
			byteArr = BitConverter.GetBytes(intData[i]);
			byteArr.CopyTo(bytesData, i * 2);
		}

		stream.Write(bytesData, 0, bytesData.Length);
	}

	static void WriteHeader(MemoryStream stream, AudioClip clip)
	{
		int hz = clip.frequency;
		int channels = clip.channels;
		int samples = clip.samples;

		Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
		stream.Write(riff, 0, 4);

		Byte[] chunkSize = BitConverter.GetBytes(stream.Length - 8);
		stream.Write(chunkSize, 0, 4);

		Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
		stream.Write(wave, 0, 4);

		Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
		stream.Write(fmt, 0, 4);

		Byte[] subChunk1 = BitConverter.GetBytes(16);
		stream.Write(subChunk1, 0, 4);

		UInt16 one = 1;
		Byte[] audioFormat = BitConverter.GetBytes(one);
		stream.Write(audioFormat, 0, 2);

		Byte[] numChannels = BitConverter.GetBytes(channels);
		stream.Write(numChannels, 0, 2);

		Byte[] sampleRate = BitConverter.GetBytes(hz);
		stream.Write(sampleRate, 0, 4);

		Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2);
		stream.Write(byteRate, 0, 4);

		UInt16 blockAlign = (ushort)(channels * 2);
		stream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

		UInt16 bps = 16;
		Byte[] bitsPerSample = BitConverter.GetBytes(bps);
		stream.Write(bitsPerSample, 0, 2);

		Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
		stream.Write(datastring, 0, 4);

		Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
		stream.Write(subChunk2, 0, 4);
	}
}
