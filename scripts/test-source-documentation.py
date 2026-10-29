#!/usr/bin/env python3
"""Mutation checks for source identity, meaning, rule membership and outcome joins."""
import importlib.util,shutil,tempfile,unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location('invoice_links',Path(__file__).with_name('check-operation-links.py'));links=importlib.util.module_from_spec(spec);spec.loader.exec_module(links)
class DocumentationTests(unittest.TestCase):
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory();self.addCleanup(self.tmp.cleanup);self.root=Path(self.tmp.name)
  shutil.copytree(links.ROOT/'src',self.root/'src',ignore=shutil.ignore_patterns('bin','obj'))
  self.base=links.generate()
 def mutate(self,relative,old,new):
  p=self.root/relative;s=p.read_text(encoding='utf-8-sig');self.assertIn(old,s);p.write_text(s.replace(old,new,1))
 def check(self):return links.SOURCE_DOCUMENTATION['project'](self.root,self.base['operations'],[])
 def test_missing_flow_identity(self):
  self.mutate('src/ECommerceStoreInvoice.Application/Descriptors/Orders/CreateOrderDescriptor.cs','[Documentation(', '[Undocumented(')
  with self.assertRaisesRegex(AssertionError,'Missing flow'):self.check()
 def test_missing_step_meaning(self):
  self.mutate('src/ECommerceStoreInvoice.Application/Descriptors/Orders/CreateOrderDescriptor.cs','description: "Validate the client identifier with the registered GUID policy."','description: ""')
  with self.assertRaisesRegex(AssertionError,'step prose'):self.check()
 def test_duplicate_qualified_step_identity(self):
  self.mutate('src/ECommerceStoreInvoice.Application/Descriptors/Orders/CreateOrderDescriptor.cs','bpmnId: "IsClientIdValid"','bpmnId: "ValidateClientId"')
  with self.assertRaisesRegex(AssertionError,'Duplicate/invalid'):self.check()
 def test_missing_rule_identity(self):
  self.mutate('src/ECommerceStoreInvoice.Domain/Validation/Concrete/Rules/Orders/OrderLinesValidationRule.cs','[Documentation(', '[Undocumented(')
  with self.assertRaisesRegex(AssertionError,'Missing source documentation'):self.check()
 def test_wrong_outcome_status(self):
  self.mutate('src/ECommerceStoreInvoice.API/Configuration/InvoiceHttpDocumentation.cs','"GetOrderById", 200','"GetOrderById", 201')
  with self.assertRaisesRegex(AssertionError,'outcome family'):self.check()
 def test_empty_outcome_effects(self):
  self.mutate('src/ECommerceStoreInvoice.API/Configuration/InvoiceHttpDocumentation.cs','"Return the complete snapshot-backed order without live catalog calls or writes."','""')
  with self.assertRaisesRegex(AssertionError,'Incomplete outcome'):self.check()
 def test_unknown_installed_rule(self):
  self.mutate('src/ECommerceStoreInvoice.Domain/Validation/Concrete/Policies/OrderValidationPolicy.cs','new OrderLinesValidationRule(', 'new UnregisteredValidationRule(')
  with self.assertRaisesRegex(AssertionError,'policy rule membership'):self.check()
if __name__=='__main__':unittest.main()
