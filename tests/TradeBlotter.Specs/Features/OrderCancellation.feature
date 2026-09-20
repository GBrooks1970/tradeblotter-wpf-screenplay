Feature: Order cancellation and state transitions
  Traders cancel pending or partially filled orders without changing their fills.
  Filled and already cancelled orders remain unchanged when cancellation is attempted.

  Scenario Outline: Apply cancellation to a selected seeded order
    Given TommyTrader has a fresh trading desktop with 4 seeded orders
    And the cancellation target "<orderId>" has status "<before>" and filled quantity <filled>
    When TommyTrader cancels order "<orderId>"
    Then AdamAuditor sees only that order's status become "<after>"
    And repeating the cancellation leaves the entire blotter unchanged

    Examples:
      | orderId       | before    | filled | after     |
      | ORD-2026-0902 | PENDING   | 0      | CANCELLED |
      | ORD-2026-0903 | PARTIAL   | 200000 | CANCELLED |
      | ORD-2026-0901 | FILLED    | 250000 | FILLED    |
      | ORD-2026-0904 | CANCELLED | 0      | CANCELLED |
