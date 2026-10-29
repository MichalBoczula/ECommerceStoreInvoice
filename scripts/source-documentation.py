"""Project authored identities and meanings; distinguish source declaration from runtime evidence."""
import json,re
from pathlib import Path
LITERAL=r'"(?:\\.|[^"\\])*"'

def attrs(name,code):
 for match in re.finditer(r'\['+name+r'\((.*?)\)\]',code,re.S):
  yield [json.loads(token) for token in re.findall(LITERAL+r'|(?<![\w.])\d+(?![\w.])',match[1])]

def slug(name):return re.sub(r'(?<!^)(?=[A-Z])','-',name).lower()

def project(root,operations,cases):
 app=root/'src/ECommerceStoreInvoice.Application';domain=root/'src/ECommerceStoreInvoice.Domain';api=root/'src/ECommerceStoreInvoice.API'
 source=lambda p:p.read_text(encoding='utf-8-sig')
 registry={}
 for path in sorted((domain/'Validation/Concrete').rglob('*.cs')):
  name=path.stem;values=list(attrs('Documentation',source(path)))
  assert len(values)==1 and len(values[0])==2 and all(values[0]),'Missing source documentation: '+str(path)
  identity,summary=values[0];assert name not in registry,'Duplicate source class: '+name
  registry[name]=dict(id=identity,name=name,summary=summary,source=path.relative_to(root).as_posix(),kind='policy' if 'Policies' in path.parts else 'policy-rule')
 policies={};rules={name:dict(meta,policyIds=[]) for name,meta in registry.items() if meta['kind']=='policy-rule'}
 for name,meta in registry.items():
  if meta['kind']!='policy':continue
  body=source(root/meta['source']);members=sorted(set(re.findall(r'new\s+(\w+ValidationRule)\s*\(',body)))
  assert members and set(members)<=rules.keys(),'Missing policy rule membership: '+name
  policies[name]=dict(meta,ruleIds=[rules[n]['id'] for n in members])
  for member in members:rules[member]['policyIds'].append(meta['id'])
 published=source(api/'Endpoints/DocumentationEndpoints.cs');flows={};inline={}
 for path in sorted((app/'Descriptors').rglob('*.cs')):
  code=source(path);name=re.search(r'FlowDescriberBase<(\w+)>',code)[1];metadata=list(attrs('Documentation',code))
  assert len(metadata)==1 and len(metadata[0])==2 and all(metadata[0]),'Missing flow identity/summary: '+name
  identity,summary=metadata[0];steps=[]
  for match in re.finditer(r'\[FlowStep\((.*?)\)\](?:\s*\[InlineRule\(.*?\)\])?\s*public\s+(?:async\s+)?[^\n]+?\s+(\w+)\s*\(',code,re.S):
   args,method=match.groups();strings=[json.loads(x) for x in re.findall(LITERAL,args)];order=re.search(r'order:\s*(\d+)',args)
   assert order and len(strings)==2 and all(strings),'Missing authored step prose: '+name+'.'+method
   steps.append(dict(id=identity+'.'+strings[0],bpmnId=strings[0],order=int(order[1]),method=method,description=strings[1]))
  assert steps and [s['order'] for s in steps]==sorted(set(s['order'] for s in steps)),'Invalid flow step order: '+name
  for values in attrs('InlineRule',code):
   assert len(values)==4 and all(values),'Invalid inline rule: '+name
   i,error,condition,effects=values;assert error not in inline,'Duplicate inline error name'
   inline[error]=dict(id=i,name=error,kind='inline-rule',condition=condition,effects=effects,source=path.relative_to(root).as_posix(),flowIds=[identity],policyIds=[])
  kinds={re.sub(r'\W','',x).lower() for x in re.findall(r'IValidationPolicy<(.+?)>',code)}
  registrations={re.sub(r'\W','',kind).lower():policy for kind,policy in re.findall(r'AddScoped<IValidationPolicy<(.+?)>,\s*(\w+)>',source(domain/'DependencyInjection.cs'))}
  assert kinds<=registrations.keys(),'Unknown flow policy: '+name
  names={registrations[k] for k in kinds}
  extra=list(attrs('UsesPolicy',code))
  assert extra==([["GuidCollectionValidationPolicy"]] if name=='CreateMultipleProductVersions' else []),'Unexpected external policy declaration'
  if name=='CreateMultipleProductVersions':
   assert 'guidCollectionValidationPolicy.Validate(request.ProductIds)' in source(app/'Services/Concrete/ProductVersions/ProductVersionService.cs'),'Bulk external policy invocation changed'
   names.add('GuidCollectionValidationPolicy')
  operation_ids=[o['operationId'] for o in operations if o['flow']==path.stem]
  provider=path.stem if path.stem.startswith('Get') else 'Get'+path.stem
  is_published='.'+provider in published
  state='direct-http-entry' if operation_ids else 'published-without-direct-http-entry' if is_published else 'internal-service-unpublished'
  flows[name]=dict(id=identity,name=name,descriptor=path.stem,summary=summary,source=path.relative_to(root).as_posix(),publication=state,published=is_published,operationIds=operation_ids,policyIds=sorted(policies[n]['id'] for n in names),ruleIds=sorted({r for n in names for r in policies[n]['ruleIds']}),steps=steps)
 for rule in inline.values():
  for flow in flows.values():
   if flow['id'] in rule['flowIds']:flow['ruleIds'].append(rule['id']);flow['ruleIds'].sort()
 outcomes={}
 for path in sorted((api/'Configuration').glob('*.cs')):
  for values in attrs('HttpOutcome',source(path)):
   assert len(values)==6 and all(str(x).strip() for x in values),'Incomplete outcome family'
   identity,op,status,condition,effects,failure=values;assert identity not in outcomes,'Duplicate outcome identity'
   flow=next((f for f in flows.values() if op in f['operationIds']),None)
   outcomes[identity]=dict(id=identity,operationId=op,status=None if status==0 else status,condition=condition,effects=effects,failure=failure,source=path.relative_to(root).as_posix(),flowId=flow['id'] if flow else None,ruleContextIds=flow['ruleIds'] if flow and status==400 else [])
 for operation in operations:
  flow=next((f for f in flows.values() if operation['operationId'] in f['operationIds']),None)
  operation['flowId']=flow['id'] if flow else None
  operation['policyIds']=flow['policyIds'] if flow else []
  operation['ruleIds']=flow['ruleIds'] if flow else []
  operation['outcomeFamilyIds']=[]
  for status in operation['responses']:
   identity='inv.outcome.'+slug(operation['operationId'])+'.'+str(status)
   assert identity in outcomes and outcomes[identity]['operationId']==operation['operationId'] and outcomes[identity]['status']==status,'Missing/mismatched outcome family: '+identity
   operation['outcomeFamilyIds'].append(identity)
  for case in operation['cases']:
   case['expectedOutcomeFamilyIds']=['inv.outcome.'+slug(operation['operationId'])+'.'+str(status) for status in case['statuses']+case['optionalStatuses']]
   case['ruleEvidenceBasis']='Rule context is declared; only response errors identify observed rules. Scenario cause/state is not a branch trace.'
 identities=[r['id'] for r in registry.values()]+[f['id'] for f in flows.values()]+[s['id'] for f in flows.values() for s in f['steps']]+list(outcomes)+[r['id'] for r in inline.values()]
 assert len(identities)==len(set(identities)) and all(re.fullmatch(r'inv\.[A-Za-z0-9.-]+',v) for v in identities),'Duplicate/invalid documentation ID'
 assert all(r['policyIds'] for r in rules.values()),'Uninstalled rule needs explicit classification'
 used={p for f in flows.values() if f['operationIds'] for p in f['policyIds']}
 for policy in policies.values():policy['httpReachability']='referenced-by-http-flow' if policy['id'] in used else 'internal-service-only'
 return dict(schemaVersion=1,basis='Authored source declarations and observed response joins; no runtime step/branch trace or retrieval implementation.',flows=list(flows.values()),policies=list(policies.values()),rules=list(rules.values())+list(inline.values()),outcomeFamilies=list(outcomes.values()),coverage=dict(flows=len(flows),publishedFlows=sum(f['published'] for f in flows.values()),httpFlows=sum(bool(f['operationIds']) for f in flows.values()),steps=sum(len(f['steps']) for f in flows.values()),policies=len(policies),policyRules=len(rules),inlineRules=len(inline),outcomeFamilies=len(outcomes)))

def check_exports(graph,flows,policies):
 actual={value['flowName']:value for wrapper in flows['flows'] for value in wrapper.values()}
 expected={flow['name']:flow for flow in graph['flows'] if flow['published']}
 assert set(actual)==set(expected),'Published flow inventory differs from authored source'
 for name,flow in expected.items():
  assert actual[name]['steps']==[dict(order=s['order'],stepName=s['description'],bpmnId=s['bpmnId']) for s in flow['steps']],'Runtime step prose/identity drift: '+name
 actual={value['policyName']:value for wrapper in policies['validations'] for value in wrapper.values()}
 assert set(actual)=={p['name'] for p in graph['policies']},'Runtime policy inventory drift'
 rules={r['id']:r for r in graph['rules']}
 for policy in graph['policies']:
  assert {r['ruleName'] for r in actual[policy['name']]['rules']}=={rules[i]['name'] for i in policy['ruleIds']},'Runtime policy membership drift: '+policy['name']
