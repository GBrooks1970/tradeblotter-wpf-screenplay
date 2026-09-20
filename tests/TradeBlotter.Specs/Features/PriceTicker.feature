Feature: Deterministic price ticker
  The simulated feed alternates known prices on the WPF dispatcher.
  Observations wait for values rather than assuming an exact timer schedule.

  Scenario Outline: Observe a complete repeating price cycle without changing orders
    Given TommyTrader has a fresh trading desktop with 4 seeded orders
    When AdamAuditor observes "<symbol>" reach "<first>" then "<second>" then "<first>"
    Then TommyTrader can select an order and the seeded blotter is unchanged

    Examples:
      | symbol  | first  | second |
      | EUR/USD | 1.0844 | 1.0840 |
      | GBP/USD | 1.2712 | 1.2718 |
