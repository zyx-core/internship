import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApplicationService } from '../../service/application.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css'] // Linked style sheet routing block
})
export class DashboardComponent implements OnInit {
  private appService = inject(ApplicationService);

  public applicationsList: any[] = [];
  public currentActivePage: number = 1;
  public totalPagesCount: number = 1;
  public stagedFileMap: Map<number, File> = new Map();

  ngOnInit(): void {
    this.fetchDataStream(this.currentActivePage);
  }

  public fetchDataStream(targetPage: number): void {
    if (targetPage < 1 || targetPage > this.totalPagesCount) return;

    this.appService.getApplications(targetPage, 5).subscribe({
      next: (response) => {
        this.applicationsList = response.data;
        this.currentActivePage = response.pageNumber;
        this.totalPagesCount = response.totalPages;
      },
      error: (err) => console.error('Failed fetching data streaming matrix', err)
    });
  }

  public handleStatusChange(id: number, targetState: string): void {
    let rejectionReason: string | null = null;

    if (targetState === 'Rejected') {
      rejectionReason = prompt('Please supply a reason for application dismissal:');
      if (rejectionReason === null) return; // Action canceled by admin
    }

    this.appService.updateStatus(id, targetState, rejectionReason).subscribe({
      next: () => {
        alert(`Application state successfully updated to ${targetState}!`);
        this.fetchDataStream(this.currentActivePage);
      },
      error: (err) => console.error('Error updating status state configuration flag', err)
    });
  }

  public onFileInterception(event: any, id: number): void {
    const interceptedFile = event.target.files[0];
    if (interceptedFile && interceptedFile.type === 'application/pdf') {
      this.stagedFileMap.set(id, interceptedFile);
    } else {
      alert('File restriction warning: Select a valid PDF asset.');
    }
  }

  public requestUpload(id: number): void {
    const activeFile = this.stagedFileMap.get(id);
    if (!activeFile) {
      alert('Please select a certificate file first.');
      return;
    }

    this.appService.uploadCertificate(id, activeFile).subscribe({
      next: () => {
        alert('Certificate attached successfully! Sync notification sent to student.');
        this.stagedFileMap.delete(id);
        this.fetchDataStream(this.currentActivePage);
      },
      error: (err) => console.error('Certificate storage dispatch failure', err)
    });
  }

  public requestDownload(id: number): void {
    this.appService.downloadCertificate(id).subscribe({
      next: (blobAsset: Blob) => {
        const standardUrl = window.URL.createObjectURL(blobAsset);
        const linkingAnchor = document.createElement('a');
        linkingAnchor.href = standardUrl;
        linkingAnchor.download = `Certificate_App_${id}.pdf`;
        linkingAnchor.click();
        window.URL.revokeObjectURL(standardUrl); // Free up browser cache memory allocations
      },
      error: (err) => alert('No downloadable asset mapped with this entry record yet.')
    });
  }
}