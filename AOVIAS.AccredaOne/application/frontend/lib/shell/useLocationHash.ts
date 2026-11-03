"use client";

import { useCallback, useEffect, useState } from "react";
import { normalizeAppHash } from "./navigation";

export function useLocationHash(): string {
  const readHash = useCallback(
    () => (typeof window !== "undefined" ? normalizeAppHash(window.location.hash) : "#overview"),
    [],
  );
  const [hash, setHash] = useState(readHash);

  useEffect(() => {
    const onHashChange = () => setHash(readHash());
    onHashChange();
    window.addEventListener("hashchange", onHashChange);
    return () => window.removeEventListener("hashchange", onHashChange);
  }, [readHash]);

  return hash;
}
