import { describe, expect, it } from "vitest";
import en from "./en.js";
import sv from "./sv.js";

describe("Swedish localization", () => {
  it("covers the English keys and preserves their placeholders", () => {
    // Arrange
    const english = en.codeFirst;
    const swedish = sv.codeFirst;

    // Act & Assert
    expect(Object.keys(swedish).sort()).toEqual(Object.keys(english).sort());
    for (const key of Object.keys(english) as (keyof typeof english)[]) {
      expect(swedish[key].match(/\{\d+\}/g) ?? []).toEqual(english[key].match(/\{\d+\}/g) ?? []);
    }
  });
});
