import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

/**
 * Marks trusted static HTML (sidebar SVG icon snippets from our registry) as safe for [innerHTML].
 * Do not use for user-supplied or API HTML.
 */
@Pipe({
  name: 'trustHtml',
  standalone: true,
})
export class TrustHtmlPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(value: string | null | undefined): SafeHtml {
    if (value == null || value === '') {
      return this.sanitizer.bypassSecurityTrustHtml('');
    }
    return this.sanitizer.bypassSecurityTrustHtml(value);
  }
}
