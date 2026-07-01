import reducer, { markOrderSettling, clearOrderSettling } from "./uiSlice";

describe("uiSlice — settling orders", () => {
  it("marks an order settling with a started-at timestamp", () => {
    const state = reducer(undefined, markOrderSettling("order-1"));
    expect(state.settlingOrders).toHaveProperty("order-1");
    expect(typeof state.settlingOrders["order-1"]).toBe("number");
  });

  it("clears a settled order", () => {
    const marked = reducer(undefined, markOrderSettling("order-1"));
    const cleared = reducer(marked, clearOrderSettling("order-1"));
    expect(cleared.settlingOrders).not.toHaveProperty("order-1");
  });

  it("tracks multiple settling orders independently", () => {
    let state = reducer(undefined, markOrderSettling("a"));
    state = reducer(state, markOrderSettling("b"));
    state = reducer(state, clearOrderSettling("a"));
    expect(state.settlingOrders).not.toHaveProperty("a");
    expect(state.settlingOrders).toHaveProperty("b");
  });
});
