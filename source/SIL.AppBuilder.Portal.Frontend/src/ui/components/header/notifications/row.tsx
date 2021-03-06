import * as React from 'react';
import { compose } from 'recompose';
import CloseIcon from '@material-ui/icons/Close';

import { attributesFor } from '@data/helpers';
import { i18nProps } from '@lib/i18n';
import { NotificationResource } from '@data';
import { withDataActions, IProvidedProps as IDataProps } from '@data/containers/resources/notification/with-data-actions';
import TimezoneLabel from '@ui/components/labels/timezone';

export interface IOwnProps {
  notification: NotificationResource;
}

export type IProps =
  & IOwnProps
  & IDataProps
  & i18nProps;

class Row extends React.Component<IProps> {
  state = { visible: false };

  toggle = () => {
    const { markAsSeen } = this.props;

    if (this.state.visible) {
      markAsSeen();
    }

    this.setState({ visible: !this.state.visible });
  }

  markAsSeen = (e) => {
    const { markAsSeen } = this.props;

    e.preventDefault();

    markAsSeen();
  }

  clear = (e) => {
    const { clear } = this.props;

    e.preventDefault();

    clear();
  }

  render() {

    const { notification } = this.props;

    const {
      title,
      description,
      time,
      isViewed
    } = attributesFor(notification);

    const viewState = isViewed ? 'seen' : 'not-seen';

    if (!notification.attributes.show) {
      return null;
    }

    return (
      <div
        data-test-notification
        className={`notification-item ${viewState}`}
        onClick={this.markAsSeen}
      >
        <a
          data-test-notification-close-one
          className='close'
          href='#'
          onClick={this.clear}
        >
          <CloseIcon />
        </a>

        <h4 className='title'>{title}</h4>
        <p className={!isViewed ? 'bold' : ''}>{description}</p>
        <p className='time'>
          <TimezoneLabel dateTime={time} />
        </p>
      </div>
    );

  }
}

export default compose(
  withDataActions
)(Row);
