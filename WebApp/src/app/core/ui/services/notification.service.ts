import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private snackBar = inject(MatSnackBar);

  private readonly defaultConfig: MatSnackBarConfig = {
    duration: 4000,
    horizontalPosition: 'right',
    verticalPosition: 'top',
  };

  showError(message: string, config?: Partial<MatSnackBarConfig>): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      panelClass: 'praman-error-snackbar',
      ...config,
    });
  }

  showSuccess(message: string, config?: Partial<MatSnackBarConfig>): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      panelClass: 'praman-success-snackbar',
      ...config,
    });
  }

  showWarning(message: string, config?: Partial<MatSnackBarConfig>): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      panelClass: 'praman-warning-snackbar',
      ...config,
    });
  }

  showInfo(message: string, config?: Partial<MatSnackBarConfig>): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      panelClass: 'praman-info-snackbar',
      ...config,
    });
  }
}
