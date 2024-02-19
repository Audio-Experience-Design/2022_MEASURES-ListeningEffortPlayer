import os
import csv
import re
import whispercpp
from whispercpp import Whisper
import argparse

whispercpp.MODELS_DIR = "./cache_of_models"
os.makedirs(whispercpp.MODELS_DIR, exist_ok=True)
print(f'UPDATED: Now saving models to: {whispercpp.MODELS_DIR}')


def postprocess(log_dir: str, csv_path: str, whisper: Whisper, model_name: str):
	'''
	Runs audio transcription on the audio files and adds a transcription file.
	'''
	# Create a generator of all wav files in logdir
	wav_files = sorted([f for f in os.listdir(log_dir) if f.endswith('.wav')])
	for wav_file in wav_files:
		print(f'Transcribing {wav_file}')
		# Transcribe the audio file
		transcription = transcribe_audio(wav_file=os.path.join(log_dir, wav_file), whisper=whisper)
		print(f'Automatic transcription (model: {transcription})')
		write_transcription(csv_path=csv_path, wav_file=wav_file, transcription=transcription, model_name=model_name)


def write_transcription(csv_path: str, wav_file: str, transcription: str, model_name: str):
	file_exists = os.path.exists(csv_path) and os.path.getsize(csv_path) > 0
	with open(csv_path, 'a') as file:
		csv_writer = csv.writer(file)
		if not file_exists:
			csv_writer.writerow(['wav_file', f'transcription {model_name}'])
		csv_writer.writerow([wav_file, transcription])


def transcribe_audio(wav_file: str, whisper: Whisper) -> str:
	'''
	Transcribes the audio file and returns the transcription.
	'''
	text_segments = whisper.extract_text(whisper.transcribe(wav_file))
	text_segments = (seg.strip() for seg in text_segments if seg.strip() != '')
	# Remove [BLANK_AUDIO] and other [COMMENTS]
	text_segments = (seg for seg in text_segments if not (seg.startswith('[') and seg.endswith(']')))
	return ' '.join(text_segments)


def get_model_options():
	return (re.search(r"ggml-(.+?)\.bin$", key).group(1) for key in whispercpp.MODELS.keys())


def main(args):
	whisper = Whisper(args.model, verbose=False)
	for log_dir in args.log_dirs:
		out_file = args.out_file if args.out_file is not None else os.path.join(log_dir, f'automatic_transcriptions.csv')
		while os.path.exists(out_file):
			out_file = out_file.replace('.csv', '_1.csv')
		print(f'Writing transcriptions for {log_dir} to {out_file}...')
		postprocess(log_dir=log_dir, csv_path=out_file, whisper=whisper, model_name=args.model)


if __name__ == '__main__':
	parser = argparse.ArgumentParser(description='Postprocesses Listening Effort logs to add transcription of audio files.')
	parser.add_argument('log_dirs', nargs='+', help='Directories containing the log wav files')
	parser.add_argument('--out-file', type=str, help='The file to save the transcriptions to. Default will save to transcriptions.csv in each provided log_dir', default=None)
	parser.add_argument('--model', help=f'The Whisper model to use for transcription. One of {list(get_model_options())} or another model (see list at https://huggingface.co/ggerganov/whisper.cpp/tree/main )', type=str, default='large-v2')
	args = parser.parse_args()
	main(args)