import { QuoteBlock } from '@/components/marketing';
import type { TestimonialReference } from '@/components/trust/models';
import { Stack, Text } from '@/components/primitives';
import { cn } from '@/lib/utils';

/**
 * Testimonial — quote presentation from TestimonialReference model.
 */
export type TestimonialProps = {
  testimonial: TestimonialReference;
  /** Visual emphasis. */
  variant?: 'default' | 'emphasized';
  className?: string;
  showRating?: boolean;
};

export function Testimonial({
  testimonial,
  variant = 'default',
  className,
  showRating = true,
}: TestimonialProps) {
  return (
    <div
      className={cn(
        variant === 'emphasized' &&
          'border-border bg-muted/30 rounded-lg border p-6 md:p-8',
        className,
      )}
    >
      <Stack gap={4}>
        {showRating && testimonial.rating ? (
          <Text
            as="p"
            size="caption"
            tone="muted"
            aria-label={`Rated ${testimonial.rating} out of 5`}
          >
            {'★'.repeat(testimonial.rating)}
            <span className="text-muted-foreground/40">
              {'★'.repeat(5 - testimonial.rating)}
            </span>
          </Text>
        ) : null}
        <QuoteBlock
          quote={testimonial.quote}
          author={testimonial.author}
          {...(testimonial.role ? { position: testimonial.role } : {})}
          {...(testimonial.organization
            ? { organization: testimonial.organization }
            : {})}
          {...(testimonial.avatar ? { avatar: testimonial.avatar } : {})}
          {...(testimonial.avatarInitials
            ? { avatarInitials: testimonial.avatarInitials }
            : {})}
        />
      </Stack>
    </div>
  );
}
