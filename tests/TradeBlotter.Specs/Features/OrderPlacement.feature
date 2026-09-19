Feature: Order placement and blotter verification
  Traders submit orders through the desktop and auditors inspect the blotter.
  MARKET orders use the deterministic mock price and remain PENDING in this SUT.

  Background:
    Given TommyTrader has a fresh trading desktop with 4 seeded orders

  Scenario: Submit a LIMIT buy order
    When TommyTrader submits a "LIMIT" order for "EUR/USD" with quantity "250000" and limit price "1.0850"
    Then AdamAuditor sees 5 orders including "ORD-2026-0905" for "EUR/USD" with quantity 250000 and price "1.0850"
    And the new order is BUY and PENDING with no filled quantity

  Scenario: Submit a MARKET buy order
    When TommyTrader submits a "MARKET" order for "GBP/USD" with quantity "100000" and limit price "unused"
    Then AdamAuditor sees 5 orders including "ORD-2026-0905" for "GBP/USD" with quantity 100000 and price "1.0842"
    And the new order is BUY and PENDING with no filled quantity

  Scenario Outline: Reject invalid order entry without changing the blotter
    When TommyTrader submits a "LIMIT" order for "EUR/USD" with quantity "<quantity>" and limit price "<price>"
    Then the validation message is "<message>"
    And dismissing the invalid order leaves the original blotter unchanged

    Examples:
      | quantity | price  | message                                       |
      | 0        | 1.0850 | Please enter a valid quantity greater than 0. |
      | 100000   | bad    | Please enter a valid price.                   |
