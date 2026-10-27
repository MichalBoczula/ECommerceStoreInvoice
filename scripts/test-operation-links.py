#!/usr/bin/env python3
"""Reject loss of baseline cases, stale links and inflated response coverage."""

import copy
import csv
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch


def load(name, filename):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(filename))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


links = load('invoice_links', 'check-operation-links.py')
contract = load('invoice_contract', 'check-openapi-contract.py')


class EvidenceLinkTests(unittest.TestCase):
    def setUp(self):
        self.cases = links.inventory()
        self.operations = links.generate()['operations']
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / 'docs').mkdir()
        for filename in ['bdd-baseline.json', 'acceptance-matrix.tsv', 'acceptance-gaps.tsv']:
            (self.root / 'docs' / filename).write_bytes((links.ROOT / 'docs' / filename).read_bytes())

    def matrix(self, filename='acceptance-matrix.tsv'):
        with (self.root / 'docs' / filename).open(newline='') as stream:
            return list(csv.DictReader(stream, delimiter='\t'))

    def write(self, rows, filename='acceptance-matrix.tsv'):
        with (self.root / 'docs' / filename).open('w', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=rows[0].keys(), delimiter='\t', lineterminator='\n')
            writer.writeheader()
            writer.writerows(rows)

    def verify(self):
        with patch.object(links, 'ROOT', self.root):
            return links.verify_matrix(self.operations, self.cases)

    def test_removed_original_case_is_rejected(self):
        with self.assertRaisesRegex(AssertionError, 'Missing original'), patch.object(links, 'ROOT', self.root):
            links.verify_baseline(self.cases[1:])

    def test_changed_behavioral_example_is_rejected(self):
        changed = copy.deepcopy(self.cases)
        next(c for c in changed if c['example'])['example']['newBehavior'] = 'changed'
        with self.assertRaisesRegex(AssertionError, 'Missing original'), patch.object(links, 'ROOT', self.root):
            links.verify_baseline(changed)

    def test_additive_identity_metadata_and_new_case_preserve_baseline(self):
        changed = copy.deepcopy(self.cases)
        for case in changed:
            if case['example'] is not None:
                case['example']['scenarioId'] = 'INV02_Metadata'
        changed.append(dict(feature='New.feature', scenario='Additional coverage', example=None))
        with patch.object(links, 'ROOT', self.root):
            links.verify_baseline(changed)

    def test_stale_outline_example_is_rejected(self):
        rows = self.matrix()
        next(r for r in rows if r['example'] != 'null')['example'] = '{"removed":"example"}'
        self.write(rows)
        with self.assertRaisesRegex(AssertionError, 'Missing or stale scenario'):
            self.verify()

    def test_missing_matrix_case_is_rejected(self):
        self.write(self.matrix()[1:])
        with self.assertRaisesRegex(AssertionError, 'Missing or stale scenario'):
            self.verify()

    def test_duplicate_scenario_identity_is_rejected(self):
        rows = self.matrix()
        rows[1]['scenarioId'] = rows[0]['scenarioId']
        self.write(rows)
        with self.assertRaisesRegex(AssertionError, 'duplicate scenarioId'):
            self.verify()

    def test_stale_route_is_rejected(self):
        rows = self.matrix()
        rows[0]['path'] += '/removed'
        self.write(rows)
        with self.assertRaisesRegex(AssertionError, 'Stale matrix route'):
            self.verify()

    def test_undeclared_scenario_status_is_rejected(self):
        rows = self.matrix()
        rows[0]['statuses'] = '502'
        self.write(rows)
        with self.assertRaisesRegex(AssertionError, 'Undeclared'):
            self.verify()

    def test_added_declared_response_needs_evidence_or_explicit_gap(self):
        self.operations[0]['responses'].append(502)
        with self.assertRaisesRegex(AssertionError, 'Undocumented or stale'):
            self.verify()

    def test_optional_race_status_does_not_close_guaranteed_coverage_gap(self):
        rows, gaps, covered = self.verify()
        self.assertTrue(any(r['optionalStatuses'] == '400,409' for r in rows))
        self.assertEqual(covered, 54)
        self.write([g for g in gaps if g['operationId'] != 'UpdateOrderStatus'], 'acceptance-gaps.tsv')
        with self.assertRaisesRegex(AssertionError, 'Undocumented or stale'):
            self.verify()

    def test_obsolete_gap_is_rejected(self):
        gaps = self.matrix('acceptance-gaps.tsv')
        gaps.append(dict(operationId='CreateOrder', status='200', task='INV-BDD-04', reason='Already covered'))
        self.write(gaps, 'acceptance-gaps.tsv')
        with self.assertRaisesRegex(AssertionError, 'Undocumented or stale'):
            self.verify()

    def test_gap_cannot_hide_multiple_statuses_in_one_cell(self):
        gaps = self.matrix('acceptance-gaps.tsv')
        gaps[0]['status'] = '500,502'
        self.write(gaps, 'acceptance-gaps.tsv')
        with self.assertRaisesRegex(AssertionError, 'exactly one'):
            self.verify()

    def test_registration_keeps_nested_schema_and_metadata_before_name(self):
        code = '''group.MapGet("/items", () => { var text = "handler; text"; return Results.Ok(text); })
            .Produces<IReadOnlyCollection<ResultDto>>(StatusCodes.Status200OK)
            .WithDescription("description; text")
            .WithName("Items").Produces<ProblemDetails>(StatusCodes.Status400BadRequest);'''
        route = links.endpoint_registrations(code, '/test')[0]
        self.assertEqual((route[0], route[1], route[2], route[4]), ('GET', '/test/items', 'Items', [200, 400]))

    def test_unnamed_and_duplicate_named_registrations_are_rejected(self):
        for metadata in ['', '.WithName("One").WithName("Two")']:
            with self.subTest(metadata=metadata), self.assertRaisesRegex(AssertionError, 'one operation name'):
                links.endpoint_registrations('group.MapGet("/x", Handler)' + metadata + ';', '/test')

    def test_missing_response_metadata_is_rejected(self):
        with self.assertRaisesRegex(AssertionError, 'response metadata'):
            links.endpoint_registrations('group.MapGet("/x", Handler).WithName("One");', '/test')

    def test_unknown_hidden_operation_is_rejected(self):
        with self.assertRaisesRegex(AssertionError, 'Unexpected hidden'):
            links.endpoint_registrations('group.MapGet("/x", Handler).ExcludeFromDescription();', '/test')

    def test_step_tables_and_docstrings_do_not_create_examples(self):
        feature = self.root / 'Parser.feature'
        feature.write_text('''Feature: Parser
  Scenario Outline: Keep real examples
    Given input
      | field | value |
      | key   | data  |
      """
      Scenario: not a scenario
      Examples:
        | fake |
        | row  |
      """
    When requested
    Then observed
    Examples:
      | parameter |
      | a\\|b     |
''')
        parsed = links.scenarios(feature)
        self.assertEqual(list(parsed), ['Keep real examples'])
        self.assertEqual(parsed['Keep real examples']['examples'], [{'parameter': 'a|b'}])

    def test_openapi_missing_or_unnamed_operation_is_rejected(self):
        document = {'paths': {}}
        for op in self.operations:
            document['paths'].setdefault(contract.normalize(op['path']), {})[op['method'].lower()] = {
                'operationId': op['operationId'],
                'responses': {str(status): {'content': {('application/problem+json' if status >= 400 else
                    'application/pdf' if op['operationId'] == 'DownloadInvoicePdf' else 'application/json'):
                    {'schema': {'type': 'object'}}}} for status in op['responses']}}
        with patch.object(contract.links, 'generate', return_value={'operations': self.operations}):
            contract.check(document)
            document['paths']['/unexpected'] = {'get': {'responses': {}}}
            with self.assertRaisesRegex(AssertionError, 'Missing OpenAPI operationId'):
                contract.check(document)
            del document['paths']['/unexpected']
            first = self.operations[0]
            del document['paths'][contract.normalize(first['path'])][first['method'].lower()]
            with self.assertRaisesRegex(AssertionError, 'inventory differs'):
                contract.check(document)


if __name__ == '__main__':
    unittest.main()
