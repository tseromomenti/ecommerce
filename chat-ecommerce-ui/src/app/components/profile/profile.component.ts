import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../services/auth.service';
import { MeResponse } from '../../models/auth.models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './profile.component.html',
  styleUrl: './profile.component.scss'
})
export class ProfileComponent implements OnInit {
  profile: MeResponse | null = null;

  constructor(private authService: AuthService) {}

  ngOnInit(): void {
    this.authService.loadMe().subscribe((me) => this.profile = me);
  }
}
