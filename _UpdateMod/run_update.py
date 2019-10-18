
import urllib2
import urllib
import re
import json
import os



###############
###functions###
###############
def get_json_object(url):
	contents = urllib2.urlopen(url).read()
	result = re.search(r'var directLinkData=({.*?});', contents, re.S)
	return json.loads(result.group(1))

def get_all_files(url, url_dir):
	result = []
	print '[get] ' + url + url_dir
	jsn = get_json_object(url + url_dir)
	dirpath = jsn['dirpath']
	for fs in jsn['content']:
		name = fs['name']
		if fs.get('size'):
			fn = dirpath + name
			result.append([fn, fs['size']])
		else:
			result.extend(get_all_files(url, dirpath + name + '/'))
	return result	



###############
#####code######
###############
CLOUD_URL = 'https://filedn.com/lWv3afQJbf54nYvdSVxmJe8'

print '[Find files]'
files = get_all_files(CLOUD_URL, '/')
print files

print '[Download files]'
for file in files:
	#if < 10MB
	if file[1] < 10000000:
		directory = os.path.dirname(file[0])
		download_directory = "Download" + directory;
		download_file = "Download" + file[0]

		if not os.path.exists(download_directory):
			os.makedirs(download_directory)

		print 'Downloading ' + file[0] + ' with size ' + str(file[1])
		urllib.urlretrieve (CLOUD_URL + file[0], download_file)
