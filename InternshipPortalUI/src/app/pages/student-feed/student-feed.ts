import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApplicationService } from '../../service/application.service';

@Component({
  selector: 'app-student-feed',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-feed.html',
  styleUrls: ['./student-feed.css']
})
export class StudentFeedComponent implements OnInit {
  private appService = inject(ApplicationService);
  public eligibleInternships: any[] = [];

  ngOnInit(): void {
    this.loadSmartFeed();
  }

  public loadSmartFeed(): void {
    this.appService.getSmartFeed().subscribe({
      next: (data) => {
        this.eligibleInternships = data;
      },
      error: (err) => {
        console.error('Failed to resolve dynamic placement feed options matrix', err);
      }
    });
  }

  public applyForRole(internshipId: number): void {
    alert(`Application submitted successfully for Internship ID: ${internshipId}!`);
  }
}