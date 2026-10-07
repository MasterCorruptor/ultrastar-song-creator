"""Validate the CPU-only research package's requirements and excluded codec modules."""
import importlib.metadata as md,json
from packaging.requirements import Requirement
issues=[]
for dist in md.distributions():
 for r in dist.requires or []:
  req=Requirement(r)
  if req.marker and not req.marker.evaluate({'extra':''}):continue
  try:
   if md.version(req.name) not in req.specifier:issues.append(str(req))
  except md.PackageNotFoundError:issues.append(str(req))
if issues:raise RuntimeError(repr(issues))
names={d.metadata['Name'].lower().replace('_','-') for d in md.distributions()}
assert 'av' not in names and 'faster-whisper' not in names
import torch,openunmix,swift_f0,ctranslate2,tokenizers
print(json.dumps({'status':'passed','requirements':'consistent','stock_pyav_present':False,'faster_whisper_package_present':False,'cpu_libraries_imported':True}))
