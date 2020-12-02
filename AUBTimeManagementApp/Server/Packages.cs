﻿//Should be the same in the server and the client
//S is for server functions, C is for client functions

public enum ServerPackages {
    SMsg,
    SLoginReply,
    SRegisterReply,
    SGetUserTeamsReply,
    SGetUserScheduleReply,
    SGetTeamScheduleReply,
    SFilterUserScheduleReply,
    SFilterTeamScheduleReply,
    SCreateTeamReply,
    SNewTeamCreated,
    SNewAdminState,
    SMemberRemoved,
    SAddMemberReply,
    SMemberAdded,
    SCreateTeamEventReply,
    SCreateUserEventReply,
    SGetUserEventReply,
    SCancelUserEventReply,
    SModifyUserEventReply,
    SGetUserInvitationsReply,
    SAcceptInvitationReply,
    SDeclineInvitationReply
}
public enum ClientPackages {
    CMsg,
    CLogin,
    CRegister,
    CGetUserTeams,
    CGetUserSchedule,
    CGetTeamSchedule,
    CGetMergedTeamSchedule,
    CFilterUserSchedule,
    CFilterTeamSchedule,
    CCreateTeam,
    CChangeAdminState,
    CRemoveMember,
    CAddMember,
    CCreateTeamEvent,
    CCreateUserEvent,
    CGetUserEvent,
    CCancelUserEvent,
    CModifyUserEvent,
    CGetUserInvitations,
    CAcceptInvitation,
    CDeclineInvitation
}