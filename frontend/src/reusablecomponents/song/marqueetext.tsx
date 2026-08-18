import { useLayoutEffect, useRef, useState } from "react";

interface MarqueeTextProps {
    text: string;
    innerClassName?: string;
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
    const Tag = Wrapper as React.ElementType;

    useLayoutEffect(() => {
        function measure() {
            const wrap = wrapperRef.current;
            const inner = innerRef.current;
            if (!wrap || !inner) return;
            const overflow = inner.scrollWidth - wrap.clientWidth;
            
            setOverflowPx(Math.max(0, overflow));
        }
        measure();

        const ro = new ResizeObserver(measure);
        if (wrapperRef.current) ro.observe(wrapperRef.current);
        if (innerRef.current) ro.observe(innerRef.current);
        return () => ro.disconnect();
    }, [text]);

    return (
        <Tag
            ref={wrapperRef as any}
            className="text-marquee"
            style={{ ["--marquee-overflow" as any]: `${overflowPx}px` }}
            title={text}
        >
            <span ref={innerRef} className={`text-marquee-inner ${innerClassName ?? ""}`}>
                {text}
            </span>
        </Tag>
    );
}