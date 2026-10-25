import { describe, expect, it } from 'vitest';
import {
  evaluateCondition,
  formatWorkflowMessage,
  parseWorkflowCondition,
} from './workflow-condition.model';

/**
 * The five fact tests are the load-bearing part of this wave: the old rules distinguished
 * `=== false` from `!x`, and `known(x) && !has(x)` from `!has(x)`, and every one of those
 * distinctions decided whether a stage announced a problem it could not actually know about.
 */
describe('evaluateCondition', () => {
  describe('fact tests', () => {
    it('matches booleans strictly, so absent is not false', () => {
      expect(evaluateCondition({ fact: 'a', is: 'true' }, { a: true })).toBe(true);
      expect(evaluateCondition({ fact: 'a', is: 'true' }, { a: false })).toBe(false);
      expect(evaluateCondition({ fact: 'a', is: 'true' }, {})).toBe(false);

      expect(evaluateCondition({ fact: 'a', is: 'false' }, { a: false })).toBe(true);
      // The distinction that keeps a page from asserting a blockage it never loaded the fact for.
      expect(evaluateCondition({ fact: 'a', is: 'false' }, {})).toBe(false);
    });

    it('separates "reported" from "reported as none"', () => {
      expect(evaluateCondition({ fact: 'n', is: 'known' }, { n: 0 })).toBe(true);
      expect(evaluateCondition({ fact: 'n', is: 'known' }, {})).toBe(false);

      expect(evaluateCondition({ fact: 'n', is: 'zero' }, { n: 0 })).toBe(true);
      expect(evaluateCondition({ fact: 'n', is: 'zero' }, { n: 3 })).toBe(false);
      // Never loaded is not zero — this is what stops an unvisited page marking a stage incomplete.
      expect(evaluateCondition({ fact: 'n', is: 'zero' }, {})).toBe(false);

      expect(evaluateCondition({ fact: 'n', is: 'positive' }, { n: 3 })).toBe(true);
      expect(evaluateCondition({ fact: 'n', is: 'positive' }, { n: 0 })).toBe(false);
      expect(evaluateCondition({ fact: 'n', is: 'positive' }, {})).toBe(false);
    });
  });

  describe('combinators', () => {
    it('requires every branch of all, and any branch of any', () => {
      const facts = { a: true, b: false };
      expect(
        evaluateCondition(
          {
            all: [
              { fact: 'a', is: 'true' },
              { fact: 'b', is: 'false' },
            ],
          },
          facts,
        ),
      ).toBe(true);
      expect(
        evaluateCondition(
          {
            all: [
              { fact: 'a', is: 'true' },
              { fact: 'b', is: 'true' },
            ],
          },
          facts,
        ),
      ).toBe(false);
      expect(
        evaluateCondition(
          {
            any: [
              { fact: 'a', is: 'true' },
              { fact: 'b', is: 'true' },
            ],
          },
          facts,
        ),
      ).toBe(true);
    });

    it('treats an empty all as true and an empty any as false, as ordinary logic does', () => {
      expect(evaluateCondition({ all: [] }, {})).toBe(true);
      expect(evaluateCondition({ any: [] }, {})).toBe(false);
    });

    it('negates, including the absent case', () => {
      expect(evaluateCondition({ not: { fact: 'a', is: 'true' } }, {})).toBe(true);
      expect(evaluateCondition({ not: { fact: 'a', is: 'true' } }, { a: true })).toBe(false);
    });
  });
});

describe('formatWorkflowMessage', () => {
  it('substitutes reported values', () => {
    expect(formatWorkflowMessage('{n} finding(s) still open', { n: 2 })).toBe(
      '2 finding(s) still open',
    );
  });

  it('leaves an unreported placeholder as written rather than rendering undefined', () => {
    expect(formatWorkflowMessage('{missing} open', {})).toBe('{missing} open');
  });

  it('passes a message with no placeholders through untouched', () => {
    expect(formatWorkflowMessage('Schedule an audit', {})).toBe('Schedule an audit');
  });
});

describe('parseWorkflowCondition', () => {
  it('accepts well-formed conditions, nested', () => {
    const parsed = parseWorkflowCondition({
      all: [{ fact: 'a', is: 'true' }, { not: { fact: 'b', is: 'positive' } }],
    });
    expect(parsed).not.toBeNull();
    expect(evaluateCondition(parsed!, { a: true })).toBe(true);
  });

  it('rejects an unknown test, so a typo cannot silently mean "never"', () => {
    expect(parseWorkflowCondition({ fact: 'a', is: 'sometimes' })).toBeNull();
  });

  it('rejects malformed shapes rather than admitting them', () => {
    expect(parseWorkflowCondition(null)).toBeNull();
    expect(parseWorkflowCondition('true')).toBeNull();
    expect(parseWorkflowCondition([])).toBeNull();
    expect(parseWorkflowCondition({ fact: 'a' })).toBeNull();
    expect(parseWorkflowCondition({ all: 'nope' })).toBeNull();
    // One bad branch invalidates the whole tree — a partially understood rule is not a rule.
    expect(parseWorkflowCondition({ all: [{ fact: 'a', is: 'true' }, { junk: 1 }] })).toBeNull();
  });
});
