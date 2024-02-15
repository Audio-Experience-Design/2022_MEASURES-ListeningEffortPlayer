import os
import csv
from tqdm import tqdm
import whispercpp
import argparse
from whispercpp.utils import MODELS_URL

# Override the model cache dir
os.environ['XDG_DATA_HOME'] = os.path.join(os.path.dirname(__file__), 'cache')

parser = argparse.ArgumentParser(description='Postprocesses Listening Effort logs to add transcription of audio files.')
parser.add_argument('log_dir', type=str, help='The directory containing the logs')
parser.add_argument('--out-file', type=str, help='The file to save the transcriptions to. Default will save to transcriptions.csv in log-dir', default=None)
parser.add_argument('--model', help=f'The Whisper model to use for transcription. Either a path to the file or one of {list(MODELS_URL.keys())} to automatically download a model', type=str, default='large-v1')
args = parser.parse_args()
whisper = None

def postprocess(log_dir: str, out_file: str):
	'''
	Runs audio transcription on the audio files and adds a transcription file.
	'''
	# Create a generator of all wav files in logdir
	wav_files = (f for f in os.listdir(logdir) if f.endswith('.wav'))
	transcriptions = []
	for wav_file in tqdm(wav_files, desc='Transcribing'):
		# Transcribe the audio file
		transcription = transcribe_audio(os.path.join(logdir, wav_file))
		write_transcription(wav_file, transcription)
	close_csv()


csv_writer = None
def write_transcription(wav_file: str, transcription: str):
	global csv_writer
	if csv_writer is None:
		csv_writer = csv.writer(open('transcriptions.csv', 'w'))
		csv_writer.writerow(['wav_file', 'transcription'])
	csv_writer.writerow([wav_file, transcription])

def close_csv():
	global csv_writer
	if csv_writer is not None:
		csv_writer.close()
		csv_writer = None

def transcribe_audio(wav_file: str) -> str:
	'''
	Transcribes the audio file and returns the transcription.
	'''
	sample_rate, audio = wavfile.read(wav_file)
	return "TODO"

def main():
	out_file = args.out_file if args.out_file is not None else os.path.join(args.log_dir, 'transcriptions.csv')
	whisper = whispercpp.Whisper.from_pretrained(args.model)
	postprocess(args.log_dir, out_file)

if __name__ == '__main__':
	main()