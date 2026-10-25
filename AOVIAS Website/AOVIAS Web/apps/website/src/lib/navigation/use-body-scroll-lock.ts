'use client';

import { useEffect } from 'react';

/**
 * Locks document scroll while a modal/drawer is open.
 * Compensates scrollbar gutter and uses fixed body for iOS stability.
 */
export function useBodyScrollLock(locked: boolean) {
  useEffect(() => {
    if (!locked) return;

    const { body, documentElement } = document;
    const scrollY = window.scrollY;
    const scrollbarGap = window.innerWidth - documentElement.clientWidth;

    const previous = {
      overflow: body.style.overflow,
      paddingRight: body.style.paddingRight,
      position: body.style.position,
      top: body.style.top,
      width: body.style.width,
    };

    body.style.overflow = 'hidden';
    body.style.position = 'fixed';
    body.style.top = `-${scrollY}px`;
    body.style.width = '100%';
    if (scrollbarGap > 0) {
      body.style.paddingRight = `${scrollbarGap}px`;
    }

    return () => {
      body.style.overflow = previous.overflow;
      body.style.paddingRight = previous.paddingRight;
      body.style.position = previous.position;
      body.style.top = previous.top;
      body.style.width = previous.width;
      window.scrollTo(0, scrollY);
    };
  }, [locked]);
}
