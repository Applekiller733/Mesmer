import { useLayoutEffect, useRef, useState } from "react";

interface MarqueeTextProps {
    text: string;
    /** Class for the inner span — typically text-marquee-title or -subtitle. */
    innerClassName?: string;
    /** Optional element type for the wrapper — defaults to div. */
    as?: keyof React.JSX.IntrinsicElements;
}

export default function MarqueeText({
    text,
    innerClassName,
    as: Wrapper = "div",
}: MarqueeTextProps) {
    const wrapperRef = useRef<HTMLElement | null>(null);
    const innerRef = useRef<HTMLSpanElement | null>(null);
    const [overflowPx, setOverflowPx] = useState(0);

    // Measure overflow whenever the text or the available width changes.
    // useLayoutEffect (not useEffect) so the measurement happens before
    // paint and we don't get a one-frame flash of unscrolled-but-clipped.
    useLayoutEffect(() => {
        function measure() {
            const wrap = wrapperRef.current;
            const inner = innerRef.current;
            if (!wrap || !inner) return;
            const overflow = inner.scrollWidth - wrap.clientWidth;
            // Clamp at 0 so non-overflowing text doesn't get a negative
            // offset (which would scroll it the wrong way).
            setOverflowPx(Math.max(0, overflow));
        }
        measure();

        // ResizeObserver keeps the overflow correct if the card or window
        // is resized. Cheap, fires only on size changes.
        const ro = new ResizeObserver(measure);
        if (wrapperRef.current) ro.observe(wrapperRef.current);
        if (innerRef.current) ro.observe(innerRef.current);
        return () => ro.disconnect();
    }, [text]);

    return (
        <Wrapper
            ref={wrapperRef as any}
            className="text-marquee"
            // CSS custom property feeds the keyframe animation's translate
            // distance. Set on the element so it inherits to the inner span.
            style={{ ["--marquee-overflow" as any]: `${overflowPx}px` }}
            title={text}
        >
            <span ref={innerRef} className={`text-marquee-inner ${innerClassName ?? ""}`}>
                {text}
            </span>
        </Wrapper>
    );
}